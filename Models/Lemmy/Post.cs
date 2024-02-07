namespace Veggy.Models.Lemmy;

public class Post
{
    [JsonPropertyName("id")]
    public int? Id { get; set; }

    [JsonPropertyName("name")]
    public required string Title { get; set; }

    [JsonPropertyName("url")]
    public string? Url { get; set; }
    
    [JsonPropertyName("body")]
    public string? Body { get; set; }        

    [JsonPropertyName("nsfw")]
    public bool IsNSFW { get; set; } = false;
}