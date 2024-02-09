using Authenticator = Veggy.Models.Gotify.Authenticator;

namespace Veggy.Services;

public class GotifyService(IDbContextFactory<VeggyContext> veggyContextFactory, ILogger<GotifyService> logger): IDisposable
{
    private IDbContextFactory<VeggyContext> VeggyContextFactory { get; } = veggyContextFactory;

    private ILogger<GotifyService> Logger { get; } = logger;

    private RestClient RestClient { get; } = new(new RestClientOptions
    { 
        Authenticator = new Authenticator(veggyContextFactory.CreateDbContext().Settings.GetSettingAsync("GotifyAppToken").GetAwaiter().GetResult().Value),
        BaseUrl = new Uri(veggyContextFactory.CreateDbContext().Settings.GetSettingAsync("GotifyUri").GetAwaiter().GetResult().Value)
    });

    private Dictionary<int, Message> Messages { get; } = [];

    private bool DisposedValue { get; set; }

    private event EventHandler<MessageEventArgs>? OnNewGotifyMessage = null;

    public async Task PushMessage(string Title, string Body, LogLevel logLevel, CancellationToken cancellationToken = new())
    {             
        try
        {  
            Logger.Log(logLevel, "{Title}: {Body}", Title, Body);

            using VeggyContext veggyContext = await VeggyContextFactory.CreateDbContextAsync(cancellationToken);

            string logFilterTokensString = (await veggyContext.Settings.GetSettingAsync("LogFilterTokens", cancellationToken: cancellationToken)).Value;
            IEnumerable<string> LogFilterTokens = logFilterTokensString.Split(";", StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

            if (LogFilterTokens.Any(token => 
                    Title.Contains(token, StringComparison.InvariantCultureIgnoreCase) || 
                    Body.Contains(token, StringComparison.InvariantCultureIgnoreCase)))
                return;

            Message newMessage;
            lock (Messages)
            {
                newMessage = new()
                {
                    InternalId = Messages.Count + 1,
                    Title = Title,
                    Body = Body,
                    Date = DateTime.Now,
                    Priority = GetGotifyPriority(logLevel)
                };

                Messages.Add(newMessage.InternalId, newMessage);
            }

            OnNewGotifyMessage?.Invoke(this, new MessageEventArgs { Message = newMessage });

            if (await CanUseGotify(cancellationToken: cancellationToken))
            {
                Setting gotifyLogLevelSetting = await veggyContext.Settings.GetSettingAsync("GotifyLogLevel", cancellationToken);
                int configuredLogLevel = int.TryParse(gotifyLogLevelSetting.Value, out int parsedLogLevel) ? parsedLogLevel : Defaults.GotifyLogLevel;

                if (configuredLogLevel <= (int)logLevel)
                    await RestClient.PostWrapper<Message, string>("message", newMessage, cancellationToken: cancellationToken);                
            }
        }            
        catch (Exception exception) when (exception is OperationCanceledException || exception is TaskCanceledException)
        {
            Logger.LogDebug("The task was canceled while pushing a message to Gotify.");
        }
        catch (CommunicationException communicationException)
        {
            Logger.LogError("There was a problem pushing a message to Gotify: {communicationException.Message};{Environment.NewLine}Call stack: {communicationException.StackTrace}",
                communicationException.Message,
                Environment.NewLine,
                communicationException.StackTrace);
        }
        catch (HttpRequestException httpRequestException)
        {
            Logger.LogError("There was a problem connecting to the configured Gotify address: {httpRequestException.Message};{Environment.NewLine}Call stack: {httpRequestException.StackTrace}",
                httpRequestException.Message,
                Environment.NewLine,
                httpRequestException.StackTrace);
        }
    }

    public async Task DeleteMessage(int? gotifyMessageId = null, int? internalId = null, CancellationToken cancellationToken = new())
    {
        if (internalId.HasValue)
            Messages.Remove(internalId.Value);

        try
        {
            if (gotifyMessageId.HasValue && await CanUseGotify(cancellationToken: cancellationToken))
                await RestClient.DeleteWrapper<string>($"message/{gotifyMessageId.Value}", cancellationToken: cancellationToken);
        }
        catch (CommunicationException communicationException)
        {
            Logger.LogError("There was a problem deleting the message to Gotify: {communicationException.Message};{Environment.NewLine}Call stack: {communicationException.StackTrace}",
                communicationException.Message,
                Environment.NewLine,
                communicationException.StackTrace);
        }
        catch (HttpRequestException httpRequestException)
        {
            Logger.LogError("There was a problem connecting to the configured Gotify address: {httpRequestException.Message};{Environment.NewLine}Call stack: {httpRequestException.StackTrace}",
                httpRequestException.Message,
                Environment.NewLine,
                httpRequestException.StackTrace);
        }
    }

    public IDbContextFactory<VeggyContext> GetVeggyContextFactory()
    {
        return VeggyContextFactory;
    }

    public async Task DeleteMessages(IDbContextFactory<VeggyContext> veggyContextFactory, CancellationToken cancellationToken = new())
    {
        Messages.Clear();

        try
        {
            if (await CanUseGotify(true, cancellationToken))
            {
                using VeggyContext veggyContext = await veggyContextFactory.CreateDbContextAsync(cancellationToken);

                Setting gotifyAppIdSetting = await veggyContext.Settings.GetSettingAsync("GotifyAppId", cancellationToken);
                int gotifyAppId = int.TryParse(gotifyAppIdSetting.Value, out int parsedGotifyAppId) ? parsedGotifyAppId : Defaults.GotifyAppId;

                await RestClient.DeleteWrapper<string>($"application/{gotifyAppId}/message", cancellationToken: cancellationToken);
            }
        }
        catch (CommunicationException communicationException)
        {
            Logger.LogError("There was a problem deleting all messages on Gotify: {communicationException.Message};{Environment.NewLine}Call stack: {communicationException.StackTrace}",
                communicationException.Message,
                Environment.NewLine,
                communicationException.StackTrace);
        }
        catch (HttpRequestException httpRequestException)
        {
            Logger.LogError("There was a problem connecting to the configured Gotify address: {httpRequestException.Message};{Environment.NewLine}Call stack: {httpRequestException.StackTrace}",
                httpRequestException.Message,
                Environment.NewLine,
                httpRequestException.StackTrace);
        }
    }

    public async Task<IEnumerable<Message>> GetMessages(CancellationToken cancellationToken = new())
    {
        try
        {
            if (await CanUseGotify(true, cancellationToken))
            {
                using VeggyContext veggyContext = await VeggyContextFactory.CreateDbContextAsync(cancellationToken);

                Setting gotifyAppIdSetting = await veggyContext.Settings.GetSettingAsync("GotifyAppId", cancellationToken);
                int gotifyAppId = int.TryParse(gotifyAppIdSetting.Value, out int parsedGotifyAppId) ? parsedGotifyAppId : Defaults.GotifyAppId;

                await RestClient.GetWrapper<PagedMessages>($"application/{gotifyAppId}/message");

                return (await RestClient.GetWrapper<PagedMessages>("", cancellationToken: cancellationToken))?.Messages ?? Messages.Values;
            }
            else
                return Messages.Values;
        }
        catch (CommunicationException communicationException)
        {
            Logger.LogError("There was a problem getting all messages on Gotify: {communicationException.Message};{Environment.NewLine}Call stack: {communicationException.StackTrace}",
                communicationException.Message,
                Environment.NewLine,
                communicationException.StackTrace);
        }
        catch (HttpRequestException httpRequestException)
        {
            Logger.LogError("There was a problem connecting to the configured Gotify address: {httpRequestException.Message};{Environment.NewLine}Call stack: {httpRequestException.StackTrace}",
                httpRequestException.Message,
                Environment.NewLine,
                httpRequestException.StackTrace);
        }

        return [];
    }

    public async Task SubscribeToStream(Func<Message, Task> callBack, CancellationToken cancellationToken = new())
    {
        if (await CanUseGotify(true, cancellationToken))
        {
            _ = Task.Run(async () =>
            {
                using VeggyContext veggyContext = await VeggyContextFactory.CreateDbContextAsync(cancellationToken);

                Setting gotifyAppIdSetting = await veggyContext.Settings.GetSettingAsync("GotifyAppId", cancellationToken);
                int gotifyAppId = int.TryParse(gotifyAppIdSetting.Value, out int parsedGotifyAppId) ? parsedGotifyAppId : Defaults.GotifyAppId;
                string gotifyAppToken = (await veggyContext.Settings.GetSettingAsync("GotifyAppToken", cancellationToken)).Value;

                ;

                using WatsonWsClient socketClient =
                    new(new Uri(RestClient.BuildUri(new RestRequest("/stream").AddQueryParameter("token", gotifyAppToken)).ToString().Replace("http", "ws")));

                async Task messageReceived(object? obj, MessageReceivedEventArgs messageReceivedEventArgs)
                {
                    Message? message = JsonSerializer.Deserialize<Message>(Encoding.UTF8.GetString(messageReceivedEventArgs.Data));

                    if (message?.AppId == gotifyAppId)
                        await callBack.Invoke(message);
                }

                void reconnect(object? _, EventArgs? __)
                {
                    if (!cancellationToken.IsCancellationRequested)
                        socketClient.Start();
                }

                socketClient.ServerDisconnected += new EventHandler(reconnect);
                socketClient.MessageReceived += new EventHandler<MessageReceivedEventArgs>(async (object? obj, MessageReceivedEventArgs messageReceivedEventArgs) => await messageReceived(obj, messageReceivedEventArgs));
                reconnect(null, null);

                while (!cancellationToken.IsCancellationRequested)
                {
                    await Task.Delay(TimeSpan.FromSeconds(5));
                }

                socketClient.Stop();
            },
            CancellationToken.None);
        }
        else
        {
            OnNewGotifyMessage += async (object? sender, MessageEventArgs messageEventArgs) =>
            {
                await callBack.Invoke(messageEventArgs.Message);
            };
        }
    }

    private async Task<bool> CanUseGotify(bool checkForAppId = false, CancellationToken cancellationToken = new())
    {
        using VeggyContext veggyContext = await VeggyContextFactory.CreateDbContextAsync(cancellationToken);

        Setting gotifyAppTokenSetting = await veggyContext.Settings.GetSettingAsync("GotifyAppToken", cancellationToken);
        Setting gotifyUriSetting = await veggyContext.Settings.GetSettingAsync("GotifyUri", cancellationToken);

        Setting gotifyAppIdSetting = await veggyContext.Settings.GetSettingAsync("GotifyAppId", cancellationToken);
        int gotifyAppId = int.TryParse(gotifyAppIdSetting.Value, out int parsedLogLevel) ? parsedLogLevel : Defaults.GotifyAppId;

        return
            !string.IsNullOrWhiteSpace(gotifyAppTokenSetting.Value) &&
            !string.IsNullOrWhiteSpace(gotifyUriSetting.Value) &&
            (!checkForAppId || gotifyAppId != -1);
    }

    public int GetGotifyPriority(LogLevel logLevel)
    {
        return logLevel switch
        {
            LogLevel.Critical => 9,
            LogLevel.Error => 8,
            LogLevel.Warning => 5,
            LogLevel.Information => 2,
            LogLevel.Debug => 1,
            LogLevel.Trace or _ => 0
        };
    }

    public LogLevel GetLogLevel(int? priority)
    {
        return priority switch
        {
            9 => LogLevel.Critical,
            8 => LogLevel.Error,
            5 or 6 or 7 => LogLevel.Warning,
            2 or 3 or 4 => LogLevel.Information,
            1 => LogLevel.Debug,
            0 or _ => LogLevel.Trace
        };
    }

    protected virtual void Dispose(bool disposing)
    {
        if (!DisposedValue)
        {
            if (disposing)
            {
                RestClient.Dispose();
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