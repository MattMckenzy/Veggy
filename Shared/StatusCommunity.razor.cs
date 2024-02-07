namespace Veggy.Shared;

public partial class StatusCommunity : IDisposable
{
    private bool disposedValue;

    public IEnumerable<KeyValuePair<Community, Status>> Statuses { get; set; } = [];

    [Inject]
    private StatusService StatusService { get; set; } = null!;

    private ModalPrompt ModalPromptReference = null!;
    
    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (firstRender)
        {
            StatusService.UpdateStatusesDelegates.TryAdd(this, UpdateStatuses);
            await UpdateStatuses();
        }

        base.OnAfterRender(firstRender);
    }

    public async Task UpdateStatuses()
    {
        Statuses = [.. StatusService.Statuses];
        await InvokeAsync(StateHasChanged);
    }

    public async Task StopCommunity(Status status)
    {
        async void stopCommunityAction()
        {
            status.CommunityCancellationTokenSource.Cancel();

            await InvokeAsync(StateHasChanged);
        }

        await ModalPromptReference!.ShowModalPrompt(new()
        {
            Title = "Cancel community processing?",
            Body = new MarkupString($"<p>Cancel processing the community: {status.Community.Name}? This community will not be processed during this round of processing and will be processed again in the next round.</p>"),
            CancelChoice = "Cancel",
            Choice = "Yes",
            ChoiceColour = "danger",
            ChoiceAction = stopCommunityAction
        });
    }

    protected virtual void Dispose(bool disposing)
    {
        if (!disposedValue)
        {
            if (disposing)
            {
                StatusService.UpdateStatusesDelegates.TryRemove(this, out _);
            }

            disposedValue = true;
        }
    }

    public void Dispose()
    {
        Dispose(disposing: true);
        GC.SuppressFinalize(this);
    }
}