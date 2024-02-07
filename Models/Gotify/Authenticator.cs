namespace Veggy.Models.Gotify;

public class Authenticator(string clientSecret) : JwtAuthenticator("")
{    
    private string ClientSecret { get; } = clientSecret;

    protected override async ValueTask<Parameter> GetAuthenticationParameter(string _)
    {
        SetBearerToken(ClientSecret);

        return await base.GetAuthenticationParameter(Token);
    }
}