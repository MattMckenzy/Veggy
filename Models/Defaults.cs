namespace Veggy.Models;

public static class Defaults
{
    // Values for defaults and parsing fallback.
    public const int ScanDelayMinutes = 10;
    public const int ParallelPostFetch = 4;
    public const int DefaultPostFetchDays = 28;
    public const int NewCommunityPostFetchDaysOverride = 36500;
    public const int GotifyAppId = -1;
    public const int GotifyLogLevel = 2;
    public const int LemmyApiLimit = 50;

    public const string ColorTheme = "Dark";
    public const string LemmyApiPath = "api/v3/";

    public const bool VeggyEnabled = true;

    /// <summary>
    /// Default settings with the key as Name and the value as Value.
    /// </summary>
    public static readonly Dictionary<string, string> Settings = new()
    {
        { "ColorTheme", ColorTheme },
        { "GotifyAppId", string.Empty },
        { "GotifyAppToken", string.Empty  },
        { "GotifyClientToken", string.Empty  },
        { "GotifyLogLevel", GotifyLogLevel.ToString() },
        { "GotifyUri", string.Empty  },
        { "LogFilterTokens", string.Empty },
        { "LemmyUri", string.Empty },
        { "LemmyApiPath", LemmyApiPath },
        { "LemmyApiLimit", LemmyApiLimit.ToString() },
        { "LemmyAdminUsername", string.Empty },
        { "LemmyAdminPassword", string.Empty },
        { "DefaultPostFetchDays", DefaultPostFetchDays.ToString() },
        { "NewCommunityPostFetchDaysOverride", NewCommunityPostFetchDaysOverride.ToString() },
        { "ScanDelayMinutes", ScanDelayMinutes.ToString() },
        { "ParallelPostFetch", ParallelPostFetch.ToString() },
        { "VeggyEnabled", VeggyEnabled.ToString() }
    };

    /// <summary>
    /// Default setting descriptions with the key as Name and the value as Description.
    /// </summary>
    public static readonly Dictionary<string, string> SettingDescriptions = new()
    {
        { "ColorTheme", $"String - The name of the color theme to use for the Veggy Web Application UI. Possible values are \"Light\" or \"Dark\". Defaults to \"${ColorTheme}\"." },
        { "GotifyAppId", "Integer - The ID of the Gotify Application in which to post Veggy notifications." },
        { "GotifyAppToken", "String - The token of the Gotifiy Application in which to post Veggy notifications." },
        { "GotifyClientToken", "String - The token of the Gotify Client in which to read Veggy notifications for the main page table." },
        { "GotifyLogLevel", $"Integer - The minimum log level to post to Gotify: Trace = 0, Debug = 1, Information = 2, Warning = 3, Error = 4, Critical = 5, and None = 6. Defaults to \"${GotifyLogLevel}\"" },
        { "GotifyUri", "String - The Uri for the Gotify instance to use for notifications." },
        { "LogFilterTokens", "String - A case-insensitive, semi-colon delimited list of tokens used to filter out unwanted log messages." },
        { "LemmyUri", "String - The Uri for the Lemmy instance to feed." },
        { "LemmyApiPath", $"String - The relative Uri path for the lemmy instance's API. Defaults to \"${LemmyApiPath}\"" },
        { "LemmyApiLimit", $"Integer - The amount of posts and comments to fetch at a time while retrieving them on a lemmy instance, maximum is usually 50. Defaults to \"${LemmyApiPath}\"" },
        { "LemmyAdminUsername", "String - The username of an admin account in the Lemmy instance to feed." },
        { "LemmyAdminUPassword", "String - The password of an admin account in the Lemmy instance to feed." },
        { "DefaultPostFetchDays", $"Integer - The default amount of days past to fetch the oldest posts when scanning a community. Defaults to \"${DefaultPostFetchDays}\"." },
        { "NewCommunityPostFetchDaysOverride", $"Integer - The amount of days past to fetch the oldest posts for a newly defined community. Defaults to \"${NewCommunityPostFetchDaysOverride}\"." },
        { "ScanDelayMinutes", $"Integer - The amount of minutes to wait between scanning communities for new posts. Defaults to \"${ScanDelayMinutes}\"." },
        { "ParallelPostFetch", $"Integer - The amount of parallel tasks to run while processing communities to fetch posts. Defaults to \"${ParallelPostFetch}\"." },
        { "VeggyEnabled", $"Boolean - True to enable community scanning and post fetching, False to disable. Defaults to \"${VeggyEnabled}\"" },
    };
}