namespace Veggy.Models.Lemmy;

public class CommunityView
{
    [JsonPropertyName("community")]
    public required Community Community { get; set; }

    [JsonPropertyName("counts")]
    public required CommunityAggregates Counts { get; set; }
}