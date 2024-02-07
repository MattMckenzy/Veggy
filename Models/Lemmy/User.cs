namespace Veggy.Models.Lemmy;

public class User
{
    [Key]
    [JsonPropertyName("username_or_email")]
    public required string Username { get; set; }

    [JsonPropertyName("password")]
    public required string Password { get; set; }

    [JsonPropertyName("jwt")]
    public string? BearerToken { get; set; }
}