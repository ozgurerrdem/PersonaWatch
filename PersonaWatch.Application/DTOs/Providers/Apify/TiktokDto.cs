using System.Text.Json.Serialization;

namespace PersonaWatch.Application.DTOs.Providers.Apify;

public class TiktokDto
{
    [JsonPropertyName("text")]
    public string? Text { get; set; }

    [JsonPropertyName("webVideoUrl")]
    public string? WebVideoUrl { get; set; }

    [JsonPropertyName("createTimeISO")]
    public string? CreateTimeISO { get; set; }

    [JsonPropertyName("diggCount")]
    public int? DiggCount { get; set; }

    [JsonPropertyName("shareCount")]
    public int? ShareCount { get; set; }

    [JsonPropertyName("playCount")]
    public int? PlayCount { get; set; } 

    [JsonPropertyName("commentCount")]
    public int? CommentCount { get; set; }

    [JsonPropertyName("collectCount")]
    public int? CollectCount { get; set; }

    [JsonPropertyName("authorMeta")]
    public TiktokAuthorMeta? AuthorMeta { get; set; }

    [JsonPropertyName("authorMeta.name")]
    public string? AuthorNameFlat { get; set; }
}

public class TiktokAuthorMeta
{
    [JsonPropertyName("name")]
    public string? Name { get; set; }

    [JsonPropertyName("avatar")]
    public string? Avatar { get; set; }
}
