namespace Veggy.Shared;

public partial class CommunitiesList
{
    #region Services Injection

    [Inject]
    private IDbContextFactory<VeggyContext> VeggyContextFactory { get; set; } = null!;

    [Inject]
    private IJSRuntime JSRuntime { get; set; } = null!;

    [Inject]
    private NavigationManager NavigationManager { get; set; } = null!;

    [Inject]
    private StorageService StorageService { get; set; } = null!;

    #endregion

    #region Parameters

    [CascadingParameter(Name = nameof(ItemKey))]
    public string? ItemKey { get; set; }

    [CascadingParameter(Name = nameof(Order))]
    public string? Order { get; set; }

    [CascadingParameter(Name = nameof(IsAscending))]
    public string? IsAscendingQuery { get; set; }

    [CascadingParameter(Name = nameof(Filters))]
    public string? FiltersQuery { get; set; }

    [CascadingParameter(Name = nameof(FromLink))]
    public bool? FromLink { get; set; }

    #endregion

    #region Private Variables

    private bool IsLoading { get; set; } = true;
    private bool IsUpdating { get; set; } = true;

    private IEnumerable<Community> CommunitiesCollection { get; set; } = [];
    private Dictionary<(string SuperCategory, string Category, string Key), string>? CommunitiesKeys { get; set; }
    private Dictionary<string, IEnumerable<string>>? CategoryKeys { get; set; }

    private Community CurrentCommunity { get; set; } = new() { OriginUrl = string.Empty, Name = string.Empty, Title = string.Empty };

    private bool CurrentCommunityKeyLocked = false;
    private bool CurrentCommunityIsDirty = false;
    private bool CurrentCommunityIsValid
    {
        get
        {
            return string.IsNullOrWhiteSpace(value: NameFeedback);
        }
    }

    private string NameFeedback = string.Empty;

    private ModalPrompt? ModalPromptReference = null;

    private bool IsAscending { get; set; } = true;
    private CommunityControlFields SelectedCommunityOrder { get; set; } = CommunityControlFields.Name;
    private Dictionary<string, string> Filters { get; set; } = [];

    #endregion

    #region Lifecycle Overrides

    protected override Task OnInitializedAsync()
    {
        Filters.TryAdd(string.Empty, string.Empty);
        foreach (CommunityControlFields communityControlFields in Enum.GetValues<CommunityControlFields>())
        {
            if (!Filters.ContainsKey(communityControlFields.ToString()))
                Filters.Add(communityControlFields.ToString(), string.Empty);
        }

        return base.OnInitializedAsync();
    }

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (firstRender)
        {
            await JSRuntime.InvokeVoidAsync("setIntegerOnly");
            await JSRuntime.InvokeVoidAsync("setNumericOnly");
            await JSRuntime.InvokeVoidAsync("setEnterNext");

            if (!FromLink ?? false)
            {
                if (string.IsNullOrWhiteSpace(Order))
                    Order = await StorageService.Get("CommunitiesList-Order");

                if (string.IsNullOrWhiteSpace(IsAscendingQuery))
                    IsAscendingQuery = await StorageService.Get("CommunitiesList-IsAscending");

                if (string.IsNullOrWhiteSpace(FiltersQuery))
                    FiltersQuery = await StorageService.Get("CommunitiesList-Filters");

                if (string.IsNullOrWhiteSpace(ItemKey))
                    ItemKey = await StorageService.Get("CommunitiesList-ItemKey");
            }
            
            SelectedCommunityOrder = Enum.TryParse(Order, out CommunityControlFields parsedOrder) ? parsedOrder : CommunityControlFields.Name;
            NavigationManager.NavigateTo(NavigationManager.GetUriWithQueryParameter(nameof(Order), SelectedCommunityOrder.ToString()), false);

            IsAscending = !bool.TryParse(IsAscendingQuery, out bool parsedIsAscending) || parsedIsAscending;
            NavigationManager.NavigateTo(NavigationManager.GetUriWithQueryParameter(nameof(IsAscending), IsAscending.ToString()), false);

            if (FiltersQuery != null)
            {
                foreach (string filterQuery in FiltersQuery.Split("|~;|", StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries))
                {
                    IEnumerable<string> keyValuePair = filterQuery.Split("|~:|", StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);
                    string? key = keyValuePair.ElementAtOrDefault(0);
                    string? value = keyValuePair.ElementAtOrDefault(1);

                    if (key != null && Filters.ContainsKey(key))
                        Filters[key] = value ?? string.Empty;
                }
            }
            NavigationManager.NavigateTo(NavigationManager.GetUriWithQueryParameter(nameof(Filters), string.Join("|~;|", Filters.Select(keyValuePair => $"{keyValuePair.Key}|~:|{keyValuePair.Value}"))), false);

            await UpdatePageState();

            if (ItemKey != null && CommunitiesCollection.Any(community => community.Name.ToString().Equals(ItemKey)))
                await LoadCommunity((string.Empty, string.Empty, ItemKey));
            else
                await CreateCommunity();

            IsLoading = false;
            await InvokeAsync(StateHasChanged);
        }

        await base.OnAfterRenderAsync(firstRender);
    }

    #endregion

    #region UI Events

    private async Task UpdateProperty(string propertyName, object value)
    {
        typeof(Community).GetProperty(propertyName)!.SetValue(CurrentCommunity, value);

        await UpdatePageState();
    }

    private async Task CreateCommunity()
    {
        async void createCommunity()
        {
            CurrentCommunityKeyLocked = false;

            await SetCurrentCommunity(new Community() { Name = string.Empty, OriginUrl = string.Empty, Title = string.Empty });
        }

        if (CurrentCommunityIsDirty)
            await ModalPromptReference!.ShowModalPrompt(new()
            {
                Title = "Discard changes?",
                Body = new MarkupString($"<p>Create a new community and discard current changes?</p>"),
                CancelChoice = "Cancel",
                Choice = "Yes",
                ChoiceColour = "danger",
                ChoiceAction = createCommunity
            });
        else
            createCommunity();
    }

    private async Task LoadCommunity((string _, string __, string Key) item)
    {
        if (item.Key.Equals(CurrentCommunity.Name, StringComparison.InvariantCultureIgnoreCase))
            return;

        Community? community = CommunitiesCollection.FirstOrDefault(community => community.Name.Equals(item.Key, StringComparison.InvariantCultureIgnoreCase));

        if (community == null)
            return;

        async void loadAction()
        {
            CurrentCommunityKeyLocked = true;

            await SetCurrentCommunity(community);

            await InvokeAsync(StateHasChanged);
        }

        if (CurrentCommunityIsDirty)
            await ModalPromptReference!.ShowModalPrompt(new()
            {
                Title = "Discard Changes?",
                Body = new MarkupString($"<p>Load the community: \"{community.Name}\" and discard current changes?</p>"),
                CancelChoice = "Cancel",
                Choice = "Yes",
                ChoiceColour = "danger",
                ChoiceAction = loadAction
            });
        else
            loadAction();
    }

    private async Task SaveCommunity()
    {
        if (!CurrentCommunityIsValid || !CurrentCommunityIsDirty)
            return;

        using VeggyContext veggyContext = await VeggyContextFactory.CreateDbContextAsync();

        Community? community = await veggyContext.Communities.FirstOrDefaultAsync(community => community.Name == CurrentCommunity.Name);

        using VeggyContext veggyContext2 = await VeggyContextFactory.CreateDbContextAsync();
        MarkupString body = default;
        if (community == null)
        {
            body = new($"<p>Succesfully added the new community \"{CurrentCommunity.Name}\"!</p>");
            veggyContext2.Communities.Add(CurrentCommunity);
        }
        else
        {
            body = new($"<p>Succesfully updated the community \"{CurrentCommunity.Name}\"!</p>");
            veggyContext2.Communities.Update(CurrentCommunity);
        }

        await veggyContext2.SaveChangesAsync();

        await ModalPromptReference!.ShowModalPrompt(new()
        {
            Title = "Saved Community",
            Body = body,
            CancelChoice = "Dismiss"
        });

        CurrentCommunityKeyLocked = true;

        await UpdatePageState();
    }

    private async Task DeleteCommunity(IEnumerable<(string SuperCategory, string Category, string Key)> items)
    {
        IEnumerable<Community> validItems = CommunitiesCollection.Where(community => items.Any(item => item.Key == community.Name));

        if (!validItems.Any())
            return;
        else if (validItems.Count() == 1)
        {
            Community community = validItems.First();

            await ModalPromptReference!.ShowModalPrompt(new()
            {
                Title = "Delete the community?",
                Body = new MarkupString($"<p>Really delete the community \"{community.Name}\"?</p>"),
                CancelChoice = "Cancel",
                Choice = "Delete",
                ChoiceColour = "danger",
                ChoiceAction = async () =>
                {
                    using VeggyContext veggyContext = await VeggyContextFactory.CreateDbContextAsync();

                    veggyContext.RemoveRange(community);
                    await veggyContext.SaveChangesAsync();

                    await ModalPromptReference.ShowModalPrompt(new()
                    {
                        Title = "Succesfully deleted!",
                        Body = new MarkupString($"<p>Succesfully deleted the community \"{community.Name}\"!</p>"),
                        CancelChoice = "Dismiss"
                    });

                    await UpdatePageState();
                }
            });
        }
        else
        {
            await ModalPromptReference!.ShowModalPrompt(new()
            {
                Title = "Delete all shown communities?",
                Body = new MarkupString($"<p>Really delete the {validItems.Count()} communities shown?</p>"),
                CancelChoice = "Cancel",
                Choice = "Delete",
                ChoiceColour = "danger",
                ChoiceAction = async () =>
                {
                    using VeggyContext veggyContext = await VeggyContextFactory.CreateDbContextAsync();

                    veggyContext.RemoveRange(validItems);
                    await veggyContext.SaveChangesAsync();

                    await ModalPromptReference.ShowModalPrompt(new()
                    {
                        Title = "Succesfully deleted!",
                        Body = new MarkupString($"<p>Succesfully deleted the {validItems.Count()} communities shown!</p>"),
                        CancelChoice = "Dismiss"
                    });

                    await UpdatePageState();
                }
            });
        }
    }

    private async Task ToggleOrder()
    {
        IsAscending = !IsAscending;
        string isAscendingQuery = IsAscending.ToString();
        NavigationManager.NavigateTo(NavigationManager.GetUriWithQueryParameter(nameof(IsAscending), isAscendingQuery), false);

        if (!string.IsNullOrWhiteSpace(isAscendingQuery))
            await StorageService.Set("CommunitiesList-IsAscending", isAscendingQuery);

        await UpdateCommunitiesKeys();
    }

    private async Task SelectCommunityOrder(CommunityControlFields communityOrder)
    {
        SelectedCommunityOrder = communityOrder;
        string communityOrderQuery = communityOrder.ToString();
        NavigationManager.NavigateTo(NavigationManager.GetUriWithQueryParameter(nameof(Order), communityOrderQuery), false);

        if (!string.IsNullOrWhiteSpace(communityOrderQuery))
            await StorageService.Set("CommunitiesList-Order", communityOrderQuery);

        await UpdateCommunitiesKeys();
    }

    private async Task FilterCommunities(Dictionary<string, string> filters)
    {
        Filters = filters;
        string filtersQuery = string.Join("|~;|", Filters.Select(keyValuePair => $"{keyValuePair.Key}|~:|{keyValuePair.Value}"));
        NavigationManager.NavigateTo(NavigationManager.GetUriWithQueryParameter(nameof(Filters), filtersQuery), false);
        
        if (!string.IsNullOrWhiteSpace(filtersQuery))
            await StorageService.Set("CommunitiesList-Filters", filtersQuery);
        
        await UpdateCommunitiesKeys();
    }


    #endregion

    #region Helper Methods

    private async Task UpdatePageState()
    {
        using VeggyContext veggyContext = await VeggyContextFactory.CreateDbContextAsync();

        ValidateCurrentCommunity();

        CurrentCommunityIsDirty = (!CurrentCommunityKeyLocked && !CompareCommunities(CurrentCommunity, new Community() { OriginUrl = string.Empty, Name = string.Empty, Title = string.Empty } )) ||
            (CurrentCommunityKeyLocked && !CompareCommunities(CurrentCommunity, veggyContext.Communities.ToArray().FirstOrDefault(community => community.Name == CurrentCommunity.Name)));

        CommunitiesCollection = [.. veggyContext.Communities];

        await UpdateCommunitiesKeys();

        IsLoading = false;
        await InvokeAsync(StateHasChanged);
    }

    private void ValidateCurrentCommunity()
    {
        // Validate Community.Name
        NameFeedback = string.Empty;
        if (!CurrentCommunityKeyLocked)
        {
            if (string.IsNullOrWhiteSpace(CurrentCommunity.Name))
                NameFeedback = "A community name is required!";
            else if (CommunitiesCollection.FirstOrDefault(community => community.Name == CurrentCommunity.Name) != null)
                NameFeedback = "The community name must be unique! ";
        }
    }

    private static bool CompareCommunities(Community? community1, Community? community2)
    {
        if (community1 == null || community2 == null)
            return false;

        return 
            community1.Name.Equals(community2.Name, StringComparison.InvariantCulture);
    }

    private async Task SetCurrentCommunity(Community community)
    {
        CurrentCommunity = community;
        NavigationManager.NavigateTo(NavigationManager.GetUriWithQueryParameter(nameof(ItemKey), community.Name), false);
        await StorageService.Set("CommunitiesList-ItemKey", community.Name);

        await UpdatePageState();
    }

    private async Task UpdateCommunitiesKeys()
    {
        IsUpdating = true;
        await InvokeAsync(StateHasChanged);

        string globalFilter = Filters[string.Empty];

        CommunitiesKeys = CommunitiesCollection.AsQueryable()
            .Where(community =>
                community.Name.Contains(globalFilter, StringComparison.CurrentCultureIgnoreCase) &&
                community.Name.Contains(Filters[CommunityControlFields.Name.ToString()], StringComparison.CurrentCultureIgnoreCase))
            .OrderBy($"{GetSortingField()} {(IsAscending ? "ASC" : "DESC")}")
            .ToDictionary(community => (string.Empty, string.Empty, community.Name), community => community.Name);
        
        IsUpdating = false;
        await InvokeAsync(StateHasChanged);
    }

    private string GetSortingField() => SelectedCommunityOrder switch
    {
        CommunityControlFields.Name or _ => nameof(Community.Name)
    };

    #endregion
}