namespace Veggy.Models.Lemmy;

public class Comment
{    
    [JsonPropertyName("content")]
    public required string Content { get; set; }

    [JsonPropertyName("post_id")]
    public required int PostId { get; set; }

    [JsonPropertyName("parent_id")]
    public int? ParentId { get; set; }

    [JsonPropertyName("published")]
    public required DateTime DatePublished { get; set; }

    [JsonPropertyName("updated")]
    public DateTime? DateUpdated { get; set; }
}
