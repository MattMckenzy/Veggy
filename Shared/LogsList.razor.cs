﻿using LogLevel = Microsoft.Extensions.Logging.LogLevel;

namespace Veggy.Shared;

public partial class LogsList
{
    [Inject]
    private GotifyService GotifyService { get; set; } = null!;

    [Inject]
    private NavigationManager NavigationManager { get; set; } = null!;

    [Inject]
    private StorageService StorageService { get; set; } = null!;

    [CascadingParameter(Name = nameof(Filter))]
    public string? Filter { get; set; }

    private bool IsLoading { get; set; } = true;
    private List<Message> Messages { get; set; } = [];
    private IEnumerable<LogLevel> FilteredLogLevels =
    [
        LogLevel.Information,
        LogLevel.Warning,
        LogLevel.Error,
        LogLevel.Critical
    ];

    protected override async Task OnInitializedAsync()
    {
        await GotifyService.SubscribeToStream(async (gotifyMessage) =>
            {
                await InvokeAsync(async () => {
                    if (!Messages.Any(message => message.InternalId.Equals(gotifyMessage.InternalId)))
                    {
                        Messages.Add(gotifyMessage);                                
                        Messages =
                        [
                            .. Messages
                                .Where(message => FilteredLogLevels.Any(logLevel => GotifyService.GetGotifyPriority(logLevel) == message.Priority))
                                .OrderByDescending(message => message.Date),
                        ];

                        await InvokeAsync(StateHasChanged);
                    }
                });
            });

        await base.OnInitializedAsync();
    }

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (firstRender)
        {
            if (string.IsNullOrWhiteSpace(Filter))
            {
                Filter = await StorageService.Get("LogsList-Filter");
                NavigationManager.NavigateTo(NavigationManager.GetUriWithQueryParameter(nameof(Filter), Filter), false);
            }

            FilteredLogLevels = Filter?.Split(",", StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries)
            .Select<string, LogLevel?>(logLevel => Enum.TryParse(logLevel, out LogLevel parsedLogLevel) ? parsedLogLevel : null)
            .Where(logLevel => logLevel != null)
            .Cast<LogLevel>()
            .ToArray() ?? 
            [
                LogLevel.Information,
                LogLevel.Warning,
                LogLevel.Error,
                LogLevel.Critical
            ];

            await UpdateLogs();

            IsLoading = false;
            await InvokeAsync(StateHasChanged);
        }

        await base.OnAfterRenderAsync(firstRender);
    }

    private async void LoggingService_LogsChanged(object sender, ChangeEventArgs changeEventArgs)
    {
        await UpdateLogs();
    }

    private async Task UpdateLogs()
    {
        Messages =
        [
            .. (await GotifyService.GetMessages())
                .Where(message => FilteredLogLevels.Any(logLevel => GotifyService.GetGotifyPriority(logLevel) == message.Priority))
                .OrderByDescending(message => message.Date)
        ];

        await InvokeAsync(StateHasChanged);
    }

    private async Task DeleteMessage(Message message)
    {
        Messages.Remove(message);

        if (message.Id != null)
            await GotifyService.DeleteMessage((int)message.Id);            
        else
            await GotifyService.DeleteMessage(message.InternalId);

        await InvokeAsync(StateHasChanged);
    }

    private async Task DeleteAllMessages()
    {
        await GotifyService.DeleteMessages(GotifyService.GetVeggyContextFactory());
        Messages.Clear();

        await InvokeAsync(StateHasChanged);
    }

    private static string GetMessageStyle(Message message)
    {
        return message.Priority switch
        {
            8 or 9 => "list-group-item-danger",
            5 or 6 or 7 => "list-group-item-warning",
            2 or 3 or 4 => "list-group-item-info",
            _ or 0 or 1 => "list-group-item-light"
        };
    }

    private async Task FilterMessages(ChangeEventArgs changeEventArgs)
    {
        if (changeEventArgs?.Value != null && changeEventArgs.Value is IEnumerable<LogLevel> selectedLogLevels)
        {
            FilteredLogLevels = selectedLogLevels;
            string filteredLogLevelsString = string.Join(",", FilteredLogLevels);
            NavigationManager.NavigateTo(NavigationManager.GetUriWithQueryParameter(nameof(Filter), filteredLogLevelsString), false);

            if (!string.IsNullOrWhiteSpace(filteredLogLevelsString))
                await StorageService.Set("LogsList-Filter", filteredLogLevelsString);
                
            await UpdateLogs();
        }
    }
}