namespace Veggy.Models;

public class Status
{
    public required Community Community { get; set; }

    public required CancellationTokenSource CommunityCancellationTokenSource { get; set; }
}