namespace Veggy.Models.Lemmy;

public class PostView
{
    [JsonPropertyName("post")]
    public required Post Post { get; set; }
}