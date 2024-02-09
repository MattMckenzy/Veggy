namespace Veggy.Models.Lemmy;

public class Post
{
    [JsonPropertyName("id")]
    public required int Id { get; set; }

    [JsonPropertyName("community_id")]
    public required int CommunityId { get; set; }

    [JsonPropertyName("name")]
    public required string Title { get; set; }

    [JsonPropertyName("url")]
    public string? Url { get; set; }

    [JsonPropertyName("body")]
    public string? Body { get; set; }

    [JsonPropertyName("nsfw")]
    public required bool IsNSFW { get; set; }

    [JsonPropertyName("published")]
    public required DateTime DatePublished { get; set; }

    [JsonPropertyName("updated")]
    public DateTime? DateUpdated { get; set; }
}