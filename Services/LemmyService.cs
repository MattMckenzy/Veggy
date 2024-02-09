using Authenticator = Veggy.Models.Lemmy.Authenticator;

namespace Veggy.Services;

public class LemmyService : IDisposable
{
    private IDbContextFactory<VeggyContext> VeggyContextFactory { get; }

    private GotifyService Logger { get; }

    private RestClient AdminRestClient { get; set; } = null!;

    private User AdminUser { get; set; } = null!;

    private bool DisposedValue { get; set; }

    public LemmyService(IDbContextFactory<VeggyContext> veggyContextFactory, GotifyService logger)
    {
        VeggyContextFactory = veggyContextFactory;
        Logger = logger;

        Task SetupTask = new(async () =>
        {
            using VeggyContext veggyContext = await VeggyContextFactory.CreateDbContextAsync();

            AdminUser = new()
            {
                Username = (await veggyContext.Settings.GetSettingAsync("LemmyAdminUsername")).Value,
                Password = (await veggyContext.Settings.GetSettingAsync("LemmyAdminPassword")).Value
            };

            AdminRestClient = await GetRestClient(AdminUser);
        });
    }

    public async Task<CommunityView?> GetCommunity(Community community, CancellationToken? cancellationToken = null)
    {
        try
        {
            CommunityView? communityView = await AdminRestClient.GetWrapper<CommunityView>(
                "community",
                [Parameter.CreateParameter("id", community.Id, ParameterType.QueryString)],
                cancellationToken);

            return communityView ?? null;
        }
        catch (CommunicationException communicationException)
        {
            await Logger.PushMessage($"There was a problem creating the community \"${community.Name}\"",
                $"{communicationException.Message};{Environment.NewLine}Call stack: {communicationException.StackTrace}",
                LogLevel.Error);
        }
        catch (HttpRequestException httpRequestException)
        {
            await Logger.PushMessage($"There was a problem connecting to the configured Lemmy address \"{AdminRestClient.Options.BaseUrl}\"",
                $"{httpRequestException.Message};{Environment.NewLine}Call stack: {httpRequestException.StackTrace}",
                LogLevel.Error);
        }

        return null;
    }

    public async Task<Community?> CreateCommunity(Community community, CancellationToken? cancellationToken = null)
    {
        try
        {
            return await AdminRestClient.PostWrapper<Community, Community>("community", community, cancellationToken: cancellationToken);
        }
        catch (CommunicationException communicationException)
        {
            await Logger.PushMessage($"There was a problem creating the community \"${community.Name}\"",
                $"{communicationException.Message};{Environment.NewLine}Call stack: {communicationException.StackTrace}",
                LogLevel.Error);
        }
        catch (HttpRequestException httpRequestException)
        {
            await Logger.PushMessage($"There was a problem connecting to the configured Lemmy address \"{AdminRestClient.Options.BaseUrl}\"",
                $"{httpRequestException.Message};{Environment.NewLine}Call stack: {httpRequestException.StackTrace}",
                LogLevel.Error);
        }

        return null;
    }

    public async Task<IEnumerable<PostView>> GetPosts(Community community, TimeSpan timeSpan, CancellationToken? cancellationToken = null)
    {
        using RestClient restClient = await GetAnonymousRestClient(community);

        try
        {
            using VeggyContext veggyContext = await VeggyContextFactory.CreateDbContextAsync();

            DateTime dateTimeLimit = DateTime.Now - timeSpan;

            Setting lemmyApiLimitSetting = await veggyContext.Settings.GetSettingAsync("LemmyApiLimit", cancellationToken ?? CancellationToken.None);
            int lemmyApiLimit = int.TryParse(lemmyApiLimitSetting.Value, out int parsedlemmyApiLimit) ? parsedlemmyApiLimit : (int)(typeof(Defaults).GetProperty("LemmyApiLimit")?.GetConstantValue() ?? 50);
        
            List<PostView> posts = [];
            IEnumerable<PostView>? newPosts = [];
            int currentPage = 1;
            do
            {
                newPosts = await restClient.GetWrapper<IEnumerable<PostView>?>(
                    "post/list",
                    [
                        Parameter.CreateParameter("community_id", community.Id, ParameterType.QueryString),
                        Parameter.CreateParameter("type_", "All", ParameterType.QueryString),
                        Parameter.CreateParameter(name: "sort", "New", ParameterType.QueryString),
                        Parameter.CreateParameter("limit", lemmyApiLimit, ParameterType.QueryString),
                        Parameter.CreateParameter("page", currentPage++, ParameterType.QueryString),
                    ],
                    cancellationToken) ?? [];

                foreach (PostView postView in newPosts)
                {
                    if (postView.Post.DateUpdated > dateTimeLimit)
                        posts.Add(postView);
                    else
                        return posts;
                }
            } 
            while (newPosts.Any());

            return posts;
        }
        catch (CommunicationException communicationException)
        {
            await Logger.PushMessage($"There was a problem while fetching posts in the community \"${community.Name}\"",
                $"{communicationException.Message};{Environment.NewLine}Call stack: {communicationException.StackTrace}",
                LogLevel.Error);
        }
        catch (HttpRequestException httpRequestException)
        {
            await Logger.PushMessage($"There was a problem connecting to the configured Lemmy address \"{restClient.Options.BaseUrl}\"",
                $"{httpRequestException.Message};{Environment.NewLine}Call stack: {httpRequestException.StackTrace}",
                LogLevel.Error);
        }

        return [];
    }

    public async Task<Post?> CreatePost(Post post, User user, CancellationToken? cancellationToken = null)
    {
        try
        {
            using RestClient restClient = await GetRestClient(user);
            PostView? postView = await restClient.PostWrapper<Post, PostView>("post", post, cancellationToken: cancellationToken);

            return postView?.Post ?? null;
        }
        catch (CommunicationException communicationException)
        {
            await Logger.PushMessage($"There was a problem while creating the post in community \"${post.CommunityId}\"",
                $"{communicationException.Message};{Environment.NewLine}Call stack: {communicationException.StackTrace}",
                LogLevel.Error);
        }
        catch (HttpRequestException httpRequestException)
        {
            await Logger.PushMessage($"There was a problem connecting to the configured Lemmy address \"{AdminRestClient.Options.BaseUrl}\"",
                $"{httpRequestException.Message};{Environment.NewLine}Call stack: {httpRequestException.StackTrace}",
                LogLevel.Error);
        }

        return null;
    }

    public async Task<IEnumerable<PostView>> GetComments(Community community, TimeSpan timeSpan, CancellationToken? cancellationToken = null)
    {
        using RestClient restClient = await GetAnonymousRestClient(community);

        try
        {
            using VeggyContext veggyContext = await VeggyContextFactory.CreateDbContextAsync();

            Setting lemmyApiLimitSetting = await veggyContext.Settings.GetSettingAsync("LemmyApiLimit", cancellationToken ?? CancellationToken.None);
            int lemmyApiLimit = int.TryParse(lemmyApiLimitSetting.Value, out int parsedlemmyApiLimit) ? parsedlemmyApiLimit : (int)(typeof(Defaults).GetProperty("LemmyApiLimit")?.GetConstantValue() ?? 50);
        
            List<PostView> comments = [];
            IEnumerable<Comment>? newComments = [];
            int currentPage = 1;
            do
            {
                newComments = await restClient.GetWrapper<IEnumerable<PostView>?>(
                    "comment/list",
                    [
                        Parameter.CreateParameter("community_id", community.Id, ParameterType.QueryString),
                        Parameter.CreateParameter("type_", "All", ParameterType.QueryString),
                        Parameter.CreateParameter(name: "sort", "New", ParameterType.QueryString),
                        Parameter.CreateParameter("limit", lemmyApiLimit, ParameterType.QueryString),
                        Parameter.CreateParameter("page", currentPage++, ParameterType.QueryString),
                    ],
                    cancellationToken) ?? [];

                foreach (Commen postView in newPosts)
                {
                    if (postView.Post?.DateUpdated > dateTimeLimit)
                        posts.Add(postView);
                    else
                        return posts;
                }
            } 
            while (newComments.Any());

            return comments;
        }
        catch (CommunicationException communicationException)
        {
            await Logger.PushMessage($"There was a problem while fetching comments in the community \"${community.Name}\"",
                $"{communicationException.Message};{Environment.NewLine}Call stack: {communicationException.StackTrace}",
                LogLevel.Error);
        }
        catch (HttpRequestException httpRequestException)
        {
            await Logger.PushMessage($"There was a problem connecting to the configured Lemmy address \"{restClient.Options.BaseUrl}\"",
                $"{httpRequestException.Message};{Environment.NewLine}Call stack: {httpRequestException.StackTrace}",
                LogLevel.Error);
        }


        return [];
    }

    public async Task CreateComment(Comment comment, User user, CancellationToken? cancellationToken = null)
    {
        try
        {
            using RestClient restClient = await GetRestClient(user);
            _ = await restClient.PostWrapper<Comment, string>("comment", comment, cancellationToken: cancellationToken);

            return;
        }
        catch (CommunicationException communicationException)
        {
            await Logger.PushMessage($"There was a problem creating the comment \"${comment.PostId}\"",
                $"{communicationException.Message};{Environment.NewLine}Call stack: {communicationException.StackTrace}",
                LogLevel.Error);
        }
        catch (HttpRequestException httpRequestException)
        {
            await Logger.PushMessage($"There was a problem connecting to the configured Lemmy address \"{AdminRestClient.Options.BaseUrl}\"",
                $"{httpRequestException.Message};{Environment.NewLine}Call stack: {httpRequestException.StackTrace}",
                LogLevel.Error);
        }
    }

    private async Task<RestClient> GetRestClient(User user)
    {
        using VeggyContext veggyContext = await VeggyContextFactory.CreateDbContextAsync();

        Uri baseApiUri = new Uri((await veggyContext.Settings.GetSettingAsync("LemmyUri")).Value)
            .Append((await VeggyContextFactory.CreateDbContext().Settings.GetSettingAsync("LemmyApiPath")).Value);

        return new RestClient(options: new RestClientOptions
        {
            Authenticator = new Authenticator(baseApiUri, user.Username, user.Password),
            BaseUrl = baseApiUri
        },
        useClientFactory: true);
    }

    private async Task<RestClient> GetAnonymousRestClient(Community community)
    {

    }

    protected virtual void Dispose(bool disposing)
    {
        if (!DisposedValue)
        {
            if (disposing)
            {
                AdminRestClient.Dispose();
            }

            DisposedValue = true;
        }
    }

    public void Dispose()
    {
        // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
        Dispose(disposing: true);
        GC.SuppressFinalize(this);
    }
}