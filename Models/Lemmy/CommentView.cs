namespace Veggy.Models.Lemmy;

public class CommentView
{
    [JsonPropertyName("comment")]
    public required Comment Comment { get; set; }
}