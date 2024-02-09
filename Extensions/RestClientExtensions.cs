namespace Veggy.Extensions;

public static class RestClientExtensions
{
    public async static Task<TResult?> GetWrapper<TResult>(this RestClient restClient, string resource, IEnumerable<Parameter>? parameters = null, CancellationToken? cancellationToken = null) =>
        await ExecuteRequest<TResult>(Method.Get, restClient, resource, parameters, cancellationToken);

    public async static Task<TResult?> PostWrapper<T, TResult>(this RestClient restClient, string resource, T requestBodyObject, IEnumerable<Parameter>? parameters = null, CancellationToken? cancellationToken = null) where T : class =>
        await ExecuteRequest<T, TResult>(Method.Post, restClient, resource, requestBodyObject, parameters, cancellationToken);

    public async static Task<TResult?> PutWrapper<T, TResult>(this RestClient restClient, string resource, T requestBodyObject, IEnumerable<Parameter>? parameters = null, CancellationToken? cancellationToken = null) where T : class =>
        await ExecuteRequest<T, TResult>(Method.Put, restClient, resource, requestBodyObject, parameters, cancellationToken);

    public async static Task<TResult?> DeleteWrapper<TResult>(this RestClient restClient, string resource, IEnumerable<Parameter>? parameters = null, CancellationToken? cancellationToken = null) =>
        await ExecuteRequest<TResult>(Method.Delete, restClient, resource, parameters, cancellationToken);

    private async static Task<TResult?> ExecuteRequest<TResult>(Method method, RestClient restClient, string resource, IEnumerable<Parameter>? parameters = null, CancellationToken? cancellationToken = null, bool reAuthenticated = false)
    {
        RestRequest restRequest = new() { Resource = resource };
        if (parameters != null)
            restRequest.AddOrUpdateParameters(parameters);
        RestResponse<TResult> restResponse = await restClient.ExecuteAsync<TResult>(restRequest, method, cancellationToken ?? CancellationToken.None);

        if (ProcessResponse(restClient, restRequest, restResponse,reAuthenticated))
            return restResponse.Data;
        else        
            return await ExecuteRequest<TResult>(method, restClient, resource, parameters, cancellationToken, true);      
    }

    private async static Task<TResult?> ExecuteRequest<T, TResult>(Method method, RestClient restClient, string resource, T requestBodyObject, IEnumerable<Parameter>? parameters = null, CancellationToken? cancellationToken = null, bool reAuthenticated = false) where T: class
    {
        RestRequest restRequest = new() { Resource = resource };
        restRequest.AddJsonBody(requestBodyObject);
        if (parameters != null)
            restRequest.AddOrUpdateParameters(parameters);
        RestResponse<TResult> restResponse = await restClient.ExecuteAsync<TResult>(restRequest, method, cancellationToken ?? CancellationToken.None);        
                
        if (ProcessResponse(restClient, restRequest, restResponse,reAuthenticated))
            return restResponse.Data;
        else        
            return await ExecuteRequest<T, TResult>(method, restClient, resource, requestBodyObject, parameters, cancellationToken, true);      
    }

    private static bool ProcessResponse(RestClient restClient, RestRequest restRequest, RestResponse restResponse, bool reAuthenticated = false)
    {
        string requestUri = restClient.BuildUri(restRequest).ToString();
        
        if (restResponse.IsSuccessful)
        {
            return true;
        }
        else if (restResponse.ErrorException != null)
        {
            throw restResponse.ErrorException;
        }
        else if (restResponse.StatusCode == HttpStatusCode.Unauthorized &&
            restClient.Options.Authenticator != null &&
            restClient.Options.Authenticator.GetType() == typeof(Models.Lemmy.Authenticator) &&
            !reAuthenticated)
        {
            ((Models.Lemmy.Authenticator)restClient.Options.Authenticator).ClearToken();
            return false;
        }
        else throw restResponse.StatusCode switch
        {
            HttpStatusCode.Unauthorized => new UnauthorizedException($"There was a downstream unauthorized communication error with status code {restResponse.StatusCode}: {restResponse.StatusDescription}")
            {
                Request = requestUri,
                StatusCode = restResponse.StatusCode,
                ReasonPhrase = restResponse.StatusDescription
            },
            HttpStatusCode.Forbidden => new ForbiddenException($"There was a downstream forbidden communication error with status code {restResponse.StatusCode}: {restResponse.StatusDescription}")
            {
                Request = requestUri,
                StatusCode = restResponse.StatusCode,
                ReasonPhrase = restResponse.StatusDescription
            },
            HttpStatusCode.NotFound => new NotFoundException($"There was an error finding the downstream entity with status code {restResponse.StatusCode}: {restResponse.StatusDescription}")
            {
                Request = requestUri,
                StatusCode = restResponse.StatusCode,
                ReasonPhrase = restResponse.StatusDescription
            },
            HttpStatusCode.Conflict => new ConflictException($"There was a downstream entity conflict error with status code {restResponse.StatusCode}: {restResponse.StatusDescription}")
            {
                Request = requestUri,
                StatusCode = restResponse.StatusCode,
                ReasonPhrase = restResponse.StatusDescription
            },
            HttpStatusCode.UnprocessableEntity => new UnprocessableEntityException($"There was a downstream unprocessable entity error with status code {restResponse.StatusCode}: {restResponse.StatusDescription}")
            {
                Request = requestUri,
                StatusCode = restResponse.StatusCode,
                ReasonPhrase = restResponse.StatusDescription
            },
            HttpStatusCode.BadRequest => new BadRequestException($"There was a downstream bad request error with status code {restResponse.StatusCode}: {restResponse.StatusDescription}")
            {
                Request = requestUri,
                StatusCode = restResponse.StatusCode,
                ReasonPhrase = restResponse.StatusDescription
            },
            _ => new CommunicationException($"There was a downstream communication error with status code {restResponse.StatusCode}: {restResponse.StatusDescription}")
            {
                Request = requestUri,
                StatusCode = restResponse.StatusCode,
                ReasonPhrase = restResponse.StatusDescription
            },
        };
    }
}