namespace Veggy.Models.Lemmy;

public class Community
{
    [JsonPropertyName("id")]
    public int? Id { get; set; }

    [Key]
    [JsonIgnore]
    public required string OriginUrl { get; set; }

    [JsonIgnore]
    public required OriginType OriginType { get; set; }

    [JsonIgnore]
    public int? PostFetchDays { get; set; }

    [JsonIgnore]
    public bool? FetchComments { get; set; }

    [JsonIgnore]
    public int? RefreshCommentsDays { get; set; }

    [JsonPropertyName("name")]
    public required string Name { get; set; }

    [JsonPropertyName("title")]
    public required string Title { get; set; }

    [JsonPropertyName("description")]
    public string? Description { get; set; }

    [JsonPropertyName("icon")]
    public string? IconUrl { get; set; }

    [JsonPropertyName("banner")]
    public string? BannerUrl { get; set; }

    [JsonPropertyName("nsfw")]
    public bool IsNSFW { get; set; } = false;

}