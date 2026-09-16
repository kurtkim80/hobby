namespace HobbyCollector.Models;

public class VideoItem
{
    public required string Id { get; set; }
    public required string Title { get; set; }
    public required string ChannelTitle { get; set; }
    public required string Category { get; set; }
    public required string Url { get; set; }
    public DateTime PublishedAt { get; set; }
    public string Description { get; set; } = string.Empty;
    public float[]? Embedding { get; set; }
    public string ThumbnailUrl => $"https://i.ytimg.com/vi/{Id}/hqdefault.jpg";

    public string ToEmbeddingText()
    {
        return $"[{Category}] {ChannelTitle}: {Title}\n{Description}";
    }
}

public class SearchResultItem
{
    public required VideoItem Video { get; set; }
    public float Similarity { get; set; }
}

public class UserKeywordItem
{
    public required string Keyword { get; set; }
    public string Category { get; set; } = string.Empty;
    public DateTime LastUsedAt { get; set; }
    public int SearchCount { get; set; }
}

public class TrendingTopic
{
    public required string Title { get; set; }
    public string ApproxTraffic { get; set; } = string.Empty;
    public string NewsTitle { get; set; } = string.Empty;
    public string NewsUrl { get; set; } = string.Empty;
    public DateTime PublishedAt { get; set; }
    public bool IsMatchedHobby { get; set; }
    public string MatchedKeyword { get; set; } = string.Empty;
}

public class ChannelConfig
{
    public required string Name { get; set; }
    public required string ChannelId { get; set; }
    public required string Category { get; set; }
    public List<string> Keywords { get; set; } = [];
}

public class AppSettings
{
    public List<ChannelConfig> Channels { get; set; } = [];
    public List<string> MonitoredKeywords { get; set; } = [];
}
