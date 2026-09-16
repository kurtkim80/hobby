using System.Text.RegularExpressions;
using System.Xml.Linq;
using HobbyCollector.Models;

namespace HobbyCollector.Services;

public class WasmCollectorService
{
    private readonly HttpClient _httpClient;

    // 브라우저 CORS 제약을 통과하기 위한 오픈 프록시 엔드포인트
    private const string CorsProxyPrefix = "https://api.allorigins.win/raw?url=";

    public WasmCollectorService(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    /// <summary>
    /// 구글 트렌드 실시간 검색어 조회 (CORS 프록시 통과)
    /// </summary>
    public async Task<List<TrendingTopic>> FetchTrendingTopicsAsync(IEnumerable<string> monitoredKeywords)
    {
        var topics = new List<TrendingTopic>();
        string targetUrl = "https://trends.google.com/trending/rss?geo=KR";
        string proxyUrl = CorsProxyPrefix + Uri.EscapeDataString(targetUrl);

        try
        {
            var xmlString = await _httpClient.GetStringAsync(proxyUrl);
            var doc = XDocument.Parse(xmlString);
            var items = doc.Descendants("item");

            var keywordList = monitoredKeywords.ToList();

            foreach (var item in items)
            {
                string title = item.Element("title")?.Value ?? string.Empty;
                var htNs = "https://trends.google.com/trending/rss";
                string traffic = item.Element(XName.Get("approx_traffic", htNs))?.Value ?? string.Empty;

                bool isMatched = false;
                string matchedKw = string.Empty;

                foreach (var kw in keywordList)
                {
                    if (title.Contains(kw, StringComparison.OrdinalIgnoreCase))
                    {
                        isMatched = true;
                        matchedKw = kw;
                        break;
                    }
                }

                if (!string.IsNullOrEmpty(title))
                {
                    topics.Add(new TrendingTopic
                    {
                        Title = title,
                        ApproxTraffic = traffic,
                        PublishedAt = DateTime.UtcNow,
                        IsMatchedHobby = isMatched,
                        MatchedKeyword = matchedKw
                    });
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[WASM] 트렌드 수집 예외: {ex.Message}");
        }

        return topics;
    }

    /// <summary>
    /// 특정 키워드로 유튜브 실시간 최신 영상 수집 (CORS 지원 Invidious 공개 API 활용)
    /// </summary>
    public async Task<List<VideoItem>> SearchYouTubeVideosAsync(string query, string category, int limit = 15)
    {
        var results = new List<VideoItem>();

        // 브라우저에서 CORS가 허용되고 정상 응답하는 인스턴스 목록
        string[] searchEndpoints = [
            "https://invidious.f5.si",
            "https://invidious.ducks.party"
        ];

        foreach (var endpoint in searchEndpoints)
        {
            try
            {
                string url = $"{endpoint}/api/v1/search?q={Uri.EscapeDataString(query)}&type=video";
                using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(8));
                var json = await _httpClient.GetStringAsync(url, cts.Token);

                using var doc = System.Text.Json.JsonDocument.Parse(json);
                if (doc.RootElement.ValueKind != System.Text.Json.JsonValueKind.Array)
                    continue;

                foreach (var item in doc.RootElement.EnumerateArray())
                {
                    if (results.Count >= limit) break;

                    string videoId = item.TryGetProperty("videoId", out var vidProp) ? vidProp.GetString() ?? "" : "";
                    if (string.IsNullOrEmpty(videoId) || videoId.Length != 11) continue;

                    string title = item.TryGetProperty("title", out var titleProp) ? titleProp.GetString() ?? query : query;
                    string channelName = item.TryGetProperty("author", out var chProp) ? chProp.GetString() ?? "" : "";
                    string description = item.TryGetProperty("description", out var descProp) ? descProp.GetString() ?? "" : "";
                    long publishedUnix = item.TryGetProperty("published", out var pubProp) ? pubProp.GetInt64() : 0;

                    DateTime publishedAt = publishedUnix > 0
                        ? DateTimeOffset.FromUnixTimeSeconds(publishedUnix).UtcDateTime
                        : DateTime.UtcNow;

                    if (description.Length > 200) description = description[..200];
                    if (string.IsNullOrEmpty(description)) description = $"{query} 관련 유튜브 영상";

                    results.Add(new VideoItem
                    {
                        Id = videoId,
                        Title = title,
                        ChannelTitle = string.IsNullOrEmpty(channelName) ? query : channelName,
                        Category = category,
                        Url = $"https://www.youtube.com/watch?v={videoId}",
                        PublishedAt = publishedAt,
                        Description = description
                    });
                }

                if (results.Count > 0)
                {
                    Console.WriteLine($"[WASM] {endpoint} 에서 '{query}' {results.Count}개 수집 성공");
                    break;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[WASM] {endpoint} 검색 실패: {ex.Message}");
            }
        }

        return results;
    }
}
