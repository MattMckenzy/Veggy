namespace Veggy.Models.Lemmy;

public class Comment
{
    [JsonPropertyName("content")]
    public required string Content { get; set; }

    [JsonPropertyName("post_id")]
    public required int PostId { get; set; }

    [JsonPropertyName("parent_id")]
    public int? ParentId { get; set; }
}
