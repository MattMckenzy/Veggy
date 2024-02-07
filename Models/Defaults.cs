namespace Veggy.Models;

public static class Defaults
{
    // Values for defaults and parsing fallback.
    public const int ScanDelayMinutes = 10;
    public const int DefaultPostFetchCount = 100;
    public const int NewCommunityPostFetchCountOverride = 10000;
    public const int GotifyAppId = -1;
    public const int GotifyLogLevel = 2;

    /// <summary>
    /// Default settings with the key as Name and the value as Value.
    /// </summary>
    public static readonly Dictionary<string, string> Settings = new()
    {
        { "ColorTheme", "Dark" },
        { "GotifyAppId", "" },
        { "GotifyAppToken", "" },
        { "GotifyClientToken", "" },
        { "GotifyLogLevel", "2" },
        { "GotifyUri", "" },
        { "LogFilterTokens", "" },
        { "DefaultPostFetchCount", "100" },
        { "NewCommunityPostFetchCountOverride", "10000" },
        { "ScanDelayMinutes", "10" },
        { "VeggyEnabled", "True" }
    };

    /// <summary>
    /// Default setting descriptions with the key as Name and the value as Description.
    /// </summary>
    public static readonly Dictionary<string, string> SettingDescriptions = new()
    {
        { "ColorTheme", "String - The name of the color theme to use for the Veggy Web Application UI. Possible values are \"Light\" or \"Dark\". Defaults to \"Dark\"." },
        { "GotifyAppId", "Integer - The ID of the Gotify Application in which to post Veggy notifications." },
        { "GotifyAppToken", "String - The token of the Gotifiy Application in which to post Veggy notifications." },
        { "GotifyClientToken", "String - The token of the Gotify Client in which to read Veggy notifications for the main page table." },
        { "GotifyLogLevel", "Integer - The minimum log level to post to Gotify: Trace = 0, Debug = 1, Information = 2, Warning = 3, Error = 4, Critical = 5, and None = 6. Defaults to 2 (Information)" },
        { "GotifyUri", "String - The Uri for the Gotify instance to use for notifications." },
        { "LogFilterTokens", "String - A case-insensitive, semi-colon delimited list of tokens used to filter out unwanted log messages." },
        { "DefaultPostFetchCount", "Integer - The default amount of posts to fetch when scanning a community. Defaults to 100." },
        { "NewCommunityPostFetchCountOverride", "Integer - The amount of posts to fetch for a newly defined community. Defaults to 10000." },
        { "ScanDelayMinutes", "Integer - The amount of minutes to wait between scanning communities for new posts. Defaults to 10." },
        { "VeggyEnabled", "Boolean - True to enable community scanning and post fetching, False to disable." },
    };
}