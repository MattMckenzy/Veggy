namespace Veggy.Models.Lemmy;

public class Authenticator(string baseUrl, string clientId, string clientSecret) : JwtAuthenticator("")
{    
    private string BaseUrl { get; } = baseUrl;
    private string ClientId { get; } = clientId;
    private string ClientSecret { get; } = clientSecret;

    public void ClearToken()
    {
        SetBearerToken(string.Empty);
    }

    protected override async ValueTask<Parameter> GetAuthenticationParameter(string _)
    {
        if (string.IsNullOrWhiteSpace(Token))
            SetBearerToken(await GetToken());

        return await base.GetAuthenticationParameter(Token);
    }

    private async Task<string> GetToken() 
    {
        RestClientOptions options = new (BaseUrl);
        
        using RestClient client = new (options);

        RestRequest request = new RestRequest("user/login")
            .AddHeader("accept", "application/json")
            .AddJsonBody(JsonSerializer.Serialize(new User { Username = ClientId, Password = ClientSecret}));

        LoginResponse? response = await client.PostAsync<LoginResponse>(request);
        return response?.BearerToken ?? throw new AuthenticationException($"Could not retrieve a bearer token for user: \"${ClientId}\"");
    }
}