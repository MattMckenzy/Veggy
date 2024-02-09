namespace Veggy.Models.Lemmy;

public class CommunityAggregates
{
    [JsonPropertyName("community_id")]
    public required decimal CommunityId { get; set; }

    [JsonPropertyName("subscribers")]
    public required decimal Subscribers { get; set; }

    [JsonPropertyName("posts")]
    public required decimal Posts { get; set; }

    [JsonPropertyName("comments")]
    public required decimal Comments { get; set; }

    [JsonPropertyName("published")]
    public required DateTime Published { get; set; }

    [JsonPropertyName("users_active_day")]
    public required decimal UsersActiveDay { get; set; }

    [JsonPropertyName("users_active_week")]
    public required decimal UsersActiveWeek { get; set; }

    [JsonPropertyName("users_active_month")]
    public required decimal UsersActiveMonth { get; set; }

    [JsonPropertyName("users_active_half_year")]
    public required decimal UsersActiveHalfYear { get; set; }
}