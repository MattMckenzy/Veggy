namespace Veggy.Services;

public class VeggyHostedService(IDbContextFactory<VeggyContext> veggyContextFactory, GotifyService gotifyService, StatusService statusService) : BackgroundService
{
    #region Private Properties

    private IDbContextFactory<VeggyContext> VeggyContextFactory { get; set; } = veggyContextFactory;
    private readonly GotifyService GotifyService = gotifyService;
    private readonly StatusService StatusService = statusService;

    private SemaphoreSlim ProcessorSemaphoreSlim { get; set; } = new(4);

    #endregion

    #region Constructor and Entry Point

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await GotifyService.PushMessage("Veggy Information", $"Veggy service started!", LogLevel.Information, stoppingToken);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                using VeggyContext veggyContext = await VeggyContextFactory.CreateDbContextAsync(stoppingToken);

                Setting veggyEnabledSetting = await veggyContext.Settings.GetSettingAsync("VeggyEnabled", stoppingToken);
                bool veggyEnabled = !bool.TryParse(veggyEnabledSetting.Value, out bool parsedVeggyEnabled) || parsedVeggyEnabled;

                if (veggyEnabled &&
                    veggyContext.Communities.Any())
                {

                    StatusService.ProcessingPercent = 0;
                    StatusService.TotalProcessingCommunities = veggyContext.Communities.Count();
                    StatusService.ProcessingCancellationTokenSource = new CancellationTokenSource();
                    StatusService.NextProcessingTime = DateTime.Now.ToLocalTime();
                    foreach (Func<Task> updateDelegate in StatusService.UpdateProcessingDelegates.Values)
                        await updateDelegate.Invoke();

                    CancellationToken processingStoppingToken =
                        CancellationTokenSource.CreateLinkedTokenSource(stoppingToken, StatusService.ProcessingCancellationTokenSource.Token).Token;

                    int parallelPostFetchCount = await ProcessorMax("ParallelPostFetch", veggyContext, processingStoppingToken);
                    ProcessorSemaphoreSlim = new(parallelPostFetchCount);

                    List<Task> communitiesProcessingTasks = [];
                    foreach (Community community in veggyContext.Communities.ToList().OrderBy(community => community.Name))
                    {
                        try
                        {
                            await ProcessorSemaphoreSlim.WaitAsync(processingStoppingToken);

                            communitiesProcessingTasks.Add(Task.Run(async () => await ProcessCommunity(community, processingStoppingToken), stoppingToken));
                        }
                        finally
                        {
                            ProcessorSemaphoreSlim.Release();
                        }
                    }

                    Task.WaitAll([.. communitiesProcessingTasks], processingStoppingToken);
                }
            }
            catch (Exception exception) when (exception is OperationCanceledException || exception is TaskCanceledException)
            {
                await GotifyService.PushMessage("Veggy Debug", $"Canceled processing.", LogLevel.Debug, stoppingToken);
            }
            catch (Exception exception)
            {
                await GotifyService.PushMessage("Veggy Critical", $"Exception processing delay: {exception.Message}{Environment.NewLine}{exception.StackTrace}", LogLevel.Critical, stoppingToken);
            }
            finally
            {
                GC.Collect(3);
                GC.WaitForPendingFinalizers();
                GC.Collect(3);
            }

            try
            {
                using VeggyContext veggyContext = await VeggyContextFactory.CreateDbContextAsync(stoppingToken);

                Setting scanDelayMinutesSetting = await veggyContext.Settings.GetSettingAsync("ScanDelayMinutes", stoppingToken);
                int scanDelayMinutes = int.TryParse(scanDelayMinutesSetting.Value, out int parsedScanDelayMinutes) ? parsedScanDelayMinutes : Defaults.ScanDelayMinutes;

                StatusService.ProcessingPercent = -1;
                StatusService.TotalProcessingCommunities = 0;
                StatusService.ProcessingCancellationTokenSource = new CancellationTokenSource();
                StatusService.NextProcessingTime = (DateTime.Now + TimeSpan.FromMinutes(scanDelayMinutes)).ToLocalTime();
                foreach (Func<Task> updateDelegate in StatusService.UpdateProcessingDelegates.Values)
                    await updateDelegate.Invoke();

                await Task.Delay(TimeSpan.FromMinutes(scanDelayMinutes),
                    CancellationTokenSource.CreateLinkedTokenSource(stoppingToken, StatusService.ProcessingCancellationTokenSource.Token).Token);

                await veggyContext.DisposeAsync();
            }
            catch (Exception exception) when (exception is OperationCanceledException || exception is TaskCanceledException)
            {
                await GotifyService.PushMessage("Veggy Debug", $"Canceled processing delay.", LogLevel.Debug, stoppingToken);
            }
            catch (Exception exception)
            {
                await GotifyService.PushMessage("Veggy Critical", $"Exception processing delay: {exception.Message}{Environment.NewLine}{exception.StackTrace}", LogLevel.Critical, stoppingToken);
            }
        }

        await GotifyService.PushMessage("Veggy Information", $"Veggy service stopped!", LogLevel.Information, stoppingToken);
    }

    #endregion

    #region Main Processing

    public async Task ProcessCommunity(Community community, CancellationToken stoppingToken)
    {
        if (stoppingToken.IsCancellationRequested)
            return;

        using VeggyContext veggyContext = await VeggyContextFactory.CreateDbContextAsync(stoppingToken);

        try
        {
            CancellationTokenSource communityStoppingTokenSource = CancellationTokenSource.CreateLinkedTokenSource(stoppingToken, new());
            CancellationToken communityStoppingToken = communityStoppingTokenSource.Token;

            if (StatusService.Statuses.TryAdd(community, new Status
            {
                Community = community,
                CommunityCancellationTokenSource = communityStoppingTokenSource
            }))
            {
                foreach (Func<Task> updateDelegate in StatusService.UpdateStatusesDelegates.Values)
                    await updateDelegate.Invoke();
            }



        }
        catch (Exception exception) when (exception is OperationCanceledException || exception is TaskCanceledException)
        {
            await GotifyService.PushMessage("Veggy Debug", $"Canceled processing of \"{community.Name}\" community from \"{community.OriginType}\".", LogLevel.Debug, stoppingToken);
        }
        catch (Exception exception)
        {
            await GotifyService.PushMessage("Veggy Error", $"Error processing \"{community.Name}\" community from \"{community.OriginType}\": {exception.Message}{System.Environment.NewLine}{exception.StackTrace}", LogLevel.Error, stoppingToken);
        }
        finally
        {
            if (StatusService.Statuses.TryRemove(community, out _))
            {
                foreach (Func<Task> updateDelegate in StatusService.UpdateStatusesDelegates.Values)
                    await updateDelegate.Invoke();
            };

            StatusService.IncrementProcessingPercent();
            foreach (Func<Task> updateDelegate in StatusService.UpdateProcessingDelegates.Values)
                await updateDelegate.Invoke();

            await veggyContext.DisposeAsync();
        }
    }

    #endregion

    #region Helper Methods

    private static async Task<int> ProcessorMax(string settingName, VeggyContext veggyContext, CancellationToken processingStoppingToken)
    {
        Setting setting = await veggyContext.Settings.GetSettingAsync(settingName, processingStoppingToken);
        int settingCount = int.TryParse(setting.Value, out int parsedSettingCount) ? parsedSettingCount : (int)(typeof(Defaults).GetProperty(settingName)?.GetConstantValue() ?? 1);
        settingCount = settingCount < 1 || settingCount > Math.Min(4, System.Environment.ProcessorCount) ? Math.Min(4, System.Environment.ProcessorCount) : settingCount;
        return settingCount;
    }

    #endregion
}