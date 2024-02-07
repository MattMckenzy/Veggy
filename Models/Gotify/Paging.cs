namespace Veggy.Models.Gotify;

public class Paging
{
    public int? Limit { get; set; }
    public string? Next { get; set; }
    public int? Since { get; set; }
    public int? Size { get; set; }
}