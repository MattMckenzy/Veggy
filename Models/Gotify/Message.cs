namespace Veggy.Models.Gotify;

public class Message
{
    [JsonIgnore]
    public int InternalId { get; set; }

    public int? AppId { get; set; }

    public int? Id { get; set; }

    public DateTime? Date { get; set; }

    public int? Priority { get; set; }

    public string? Title { get; set; }

    [JsonPropertyName("Message")]
    public string? Body { get; set; }
}