using System.Net.Http.Json;
using System.Numerics.Tensors;
using System.Text.Json;
using HobbyCollector.Models;
using Microsoft.JSInterop;

namespace HobbyCollector.Services;

public class WasmVectorStore
{
    private readonly HttpClient _http;
    private readonly IJSRuntime _js;
    private readonly LocalEmbeddingService _embedder;
    private readonly List<VideoItem> _videos = [];
    private readonly List<UserKeywordItem> _userKeywords = [];
    private bool _initialized = false;

    private const string LocalStorageKeyVideos = "hobby_custom_videos";
    private const string LocalStorageKeyKeywords = "hobby_user_keywords";
    private const string LocalStorageKeyDeletedVideos = "hobby_deleted_video_ids";
    private readonly HashSet<string> _deletedVideoIds = [];

    public WasmVectorStore(HttpClient http, IJSRuntime js, LocalEmbeddingService embedder)
    {
        _http = http;
        _js = js;
        _embedder = embedder;
    }

    public async Task InitializeAsync()
    {
        if (_initialized) return;
        await LoadInternalAsync(forceReload: false);
    }

    public async Task ReloadDataAsync()
    {
        await LoadInternalAsync(forceReload: true);
    }

    private async Task LoadInternalAsync(bool forceReload)
    {
        if (forceReload)
        {
            _videos.Clear();
            _userKeywords.Clear();
            _deletedVideoIds.Clear();
            _initialized = false;
        }

        // 0. 삭제된 영상 ID 로드
        try
        {
            string? deletedJson = await _js.InvokeAsync<string?>("localStorage.getItem", LocalStorageKeyDeletedVideos);
            if (!string.IsNullOrEmpty(deletedJson))
            {
                var deletedList = JsonSerializer.Deserialize<List<string>>(deletedJson);
                if (deletedList != null)
                {
                    foreach (var id in deletedList) _deletedVideoIds.Add(id);
                }
            }
        }
        catch { }

        // 1. seed_videos.json 정적 데이터 로드
        try
        {
            var seeds = await _http.GetFromJsonAsync<List<VideoItem>>("data/seed_videos.json");
            if (seeds != null)
            {
                foreach (var v in seeds)
                {
                    if (_deletedVideoIds.Contains(v.Id)) continue;
                    v.Embedding = _embedder.GenerateEmbedding(v.ToEmbeddingText());
                    _videos.Add(v);
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[WASM] Seed 로드 실패: {ex.Message}");
        }

        // 2. 브라우저 localStorage에서 커스텀 수집 영상 로드
        try
        {
            string? customJson = await _js.InvokeAsync<string?>("localStorage.getItem", LocalStorageKeyVideos);
            if (!string.IsNullOrEmpty(customJson))
            {
                var customs = JsonSerializer.Deserialize<List<VideoItem>>(customJson);
                if (customs != null)
                {
                    foreach (var c in customs)
                    {
                        if (_deletedVideoIds.Contains(c.Id)) continue;
                        if (!_videos.Any(v => v.Id == c.Id))
                        {
                            c.Embedding = _embedder.GenerateEmbedding(c.ToEmbeddingText());
                            _videos.Add(c);
                        }
                    }
                }
            }
        }
        catch { }

        // 3. 브라우저 localStorage에서 사용자 키워드 로드
        try
        {
            string? kwJson = await _js.InvokeAsync<string?>("localStorage.getItem", LocalStorageKeyKeywords);
            if (!string.IsNullOrEmpty(kwJson))
            {
                var kws = JsonSerializer.Deserialize<List<UserKeywordItem>>(kwJson);
                if (kws != null)
                {
                    _userKeywords.AddRange(kws);
                }
            }
        }
        catch { }

        _initialized = true;
    }

    public async Task<int> SaveVideosAsync(IEnumerable<VideoItem> items)
    {
        await InitializeAsync();
        int added = 0;
        var toPersist = new List<VideoItem>();

        foreach (var item in items)
        {
            if (_deletedVideoIds.Contains(item.Id)) continue;
            if (!_videos.Any(v => v.Id == item.Id))
            {
                item.Embedding = _embedder.GenerateEmbedding(item.ToEmbeddingText());
                _videos.Insert(0, item);
                toPersist.Add(item);
                added++;
            }
        }

        if (added > 0)
        {
            try
            {
                string? existingJson = await _js.InvokeAsync<string?>("localStorage.getItem", LocalStorageKeyVideos);
                var existing = !string.IsNullOrEmpty(existingJson) 
                    ? JsonSerializer.Deserialize<List<VideoItem>>(existingJson) ?? [] 
                    : [];

                existing.AddRange(toPersist);
                // 최대 500개 보관
                if (existing.Count > 500) existing = existing.TakeLast(500).ToList();

                await _js.InvokeVoidAsync("localStorage.setItem", LocalStorageKeyVideos, JsonSerializer.Serialize(existing));
            }
            catch { }
        }

        return added;
    }

    public async Task<List<SearchResultItem>> SearchAsync(string query, int topK = 30, string? categoryFilter = null)
    {
        await InitializeAsync();
        if (string.IsNullOrWhiteSpace(query)) return [];

        float[] queryVector = _embedder.GenerateEmbedding(query);

        var candidates = _videos.AsEnumerable();
        if (!string.IsNullOrEmpty(categoryFilter))
        {
            candidates = candidates.Where(v => v.Category == categoryFilter);
        }

        return candidates
            .Where(v => v.Embedding != null && v.Embedding.Length == queryVector.Length)
            .Select(v => new SearchResultItem
            {
                Video = v,
                Similarity = TensorPrimitives.CosineSimilarity(queryVector, v.Embedding!)
            })
            .OrderByDescending(x => x.Similarity)
            .Take(topK)
            .ToList();
    }

    public async Task<List<VideoItem>> GetRecentVideosAsync(int limit = 60, string? category = null)
    {
        await InitializeAsync();
        var q = _videos.AsEnumerable();
        if (!string.IsNullOrEmpty(category))
        {
            q = q.Where(v => v.Category == category);
        }
        return q.OrderByDescending(v => v.PublishedAt).Take(limit).ToList();
    }

    public async Task<DateTime?> GetLastPublishedTimeAsync()
    {
        await InitializeAsync();
        return _videos.Count > 0 ? _videos.Max(v => v.PublishedAt) : null;
    }

    public async Task<DateTime?> GetLastSyncTimeAsync()
    {
        await InitializeAsync();
        try
        {
            string? syncStr = await _js.InvokeAsync<string?>("localStorage.getItem", "hobby_last_sync_time");
            if (!string.IsNullOrEmpty(syncStr) && DateTime.TryParse(syncStr, out var dt))
            {
                return dt;
            }
        }
        catch { }
        return null;
    }

    public async Task RecordSyncTimeAsync()
    {
        try
        {
            await _js.InvokeVoidAsync("localStorage.setItem", "hobby_last_sync_time", DateTime.UtcNow.ToString("o"));
        }
        catch { }
    }

    public async Task<Dictionary<string, int>> GetStatisticsAsync()
    {
        await InitializeAsync();
        return _videos
            .GroupBy(v => v.Category)
            .ToDictionary(g => g.Key, g => g.Count());
    }

    public async Task<List<UserKeywordItem>> GetUserKeywordsAsync()
    {
        await InitializeAsync();
        return _userKeywords.OrderByDescending(k => k.LastUsedAt).ToList();
    }

    public async Task SaveUserKeywordAsync(string keyword, string? category = null)
    {
        await InitializeAsync();
        if (string.IsNullOrWhiteSpace(keyword)) return;

        string cleanKw = keyword.Trim();
        var existing = _userKeywords.FirstOrDefault(k => k.Keyword.Equals(cleanKw, StringComparison.OrdinalIgnoreCase));
        if (existing != null)
        {
            existing.SearchCount++;
            existing.LastUsedAt = DateTime.UtcNow;
            if (!string.IsNullOrEmpty(category)) existing.Category = category;
        }
        else
        {
            _userKeywords.Insert(0, new UserKeywordItem
            {
                Keyword = cleanKw,
                Category = category ?? string.Empty,
                LastUsedAt = DateTime.UtcNow,
                SearchCount = 1
            });
        }

        try
        {
            await _js.InvokeVoidAsync("localStorage.setItem", LocalStorageKeyKeywords, JsonSerializer.Serialize(_userKeywords));
        }
        catch { }
    }

    public async Task<bool> DeleteUserKeywordAsync(string keyword)
    {
        await InitializeAsync();
        int removed = _userKeywords.RemoveAll(k => k.Keyword.Equals(keyword.Trim(), StringComparison.OrdinalIgnoreCase));
        if (removed > 0)
        {
            try
            {
                await _js.InvokeVoidAsync("localStorage.setItem", LocalStorageKeyKeywords, JsonSerializer.Serialize(_userKeywords));
            }
            catch { }
            return true;
        }
        return false;
    }

    public async Task<bool> DeleteVideoAsync(string videoId)
    {
        await InitializeAsync();
        int removed = _videos.RemoveAll(v => v.Id == videoId);
        _deletedVideoIds.Add(videoId);

        try
        {
            await _js.InvokeVoidAsync("localStorage.setItem", LocalStorageKeyDeletedVideos, JsonSerializer.Serialize(_deletedVideoIds.ToList()));

            // 커스텀 수집 목록에서도 제거
            string? customJson = await _js.InvokeAsync<string?>("localStorage.getItem", LocalStorageKeyVideos);
            if (!string.IsNullOrEmpty(customJson))
            {
                var customs = JsonSerializer.Deserialize<List<VideoItem>>(customJson);
                if (customs != null)
                {
                    customs.RemoveAll(v => v.Id == videoId);
                    await _js.InvokeVoidAsync("localStorage.setItem", LocalStorageKeyVideos, JsonSerializer.Serialize(customs));
                }
            }
        }
        catch { }

        return removed > 0;
    }

    public async Task<int> GetDeletedCountAsync()
    {
        await InitializeAsync();
        return _deletedVideoIds.Count;
    }

    public async Task RestoreAllDeletedVideosAsync()
    {
        await InitializeAsync();
        _deletedVideoIds.Clear();
        try
        {
            await _js.InvokeVoidAsync("localStorage.removeItem", LocalStorageKeyDeletedVideos);
        }
        catch { }
        await ReloadDataAsync();
    }
}
