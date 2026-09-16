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
    /// 특정 키워드로 유튜브 실시간 최신 영상 수집 (CORS 프록시 통과)
    /// </summary>
    public async Task<List<VideoItem>> SearchYouTubeVideosAsync(string query, string category, int limit = 15)
    {
        var results = new List<VideoItem>();
        string targetUrl = $"https://www.youtube.com/results?search_query={Uri.EscapeDataString(query)}&sp=CAISAhAB";
        string proxyUrl = CorsProxyPrefix + Uri.EscapeDataString(targetUrl);

        try
        {
            string html = await _httpClient.GetStringAsync(proxyUrl);

            var videoIdMatches = Regex.Matches(html, @"\""videoId\""\s*:\s*\""([a-zA-Z0-9_-]{11})\""");
            var titleMatches = Regex.Matches(html, @"\""title\""\s*:\s*\{\s*\""runs\""\s*:\s*\[\s*\{\s*\""text\""\s*:\s*\""(.*?)\""");

            var seenIds = new HashSet<string>();

            for (int i = 0; i < videoIdMatches.Count && results.Count < limit; i++)
            {
                string id = videoIdMatches[i].Groups[1].Value;
                if (!seenIds.Add(id)) continue;

                string title = (i < titleMatches.Count) ? titleMatches[i].Groups[1].Value : $"{query} 관련 영상";
                title = Regex.Unescape(title);

                results.Add(new VideoItem
                {
                    Id = id,
                    Title = title,
                    ChannelTitle = query,
                    Category = category,
                    Url = $"https://www.youtube.com/watch?v={id}",
                    PublishedAt = DateTime.UtcNow.AddHours(-1 * (i + 1)),
                    Description = $"YouTube 검색: {query}"
                });
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[WASM] 검색 수집 실패: {ex.Message}");
        }

        return results;
    }
}
