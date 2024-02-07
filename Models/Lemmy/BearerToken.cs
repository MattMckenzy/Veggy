namespace Veggy.Models.Lemmy;

public class LoginResponse
{    
    [JsonPropertyName("jwt")]
    public string? BearerToken { get; set; }
}