namespace Veggy.Pages;

[ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
[IgnoreAntiforgeryToken]
public class HostModel(IDbContextFactory<VeggyContext> dbContextFactory) : PageModel
{
    private IDbContextFactory<VeggyContext> DbContextFactory { get; set; } = dbContextFactory;

    public string ColorTheme = "dark";

    public async void OnGet()
    {
        VeggyContext veggyContext = await DbContextFactory.CreateDbContextAsync();
        ColorTheme = (await veggyContext.Settings.GetSettingAsync("ColorTheme")).Value.ToLower();                    
    }
}