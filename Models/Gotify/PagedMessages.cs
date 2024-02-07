namespace Veggy.Models.Gotify;

public class PagedMessages
{
    public IEnumerable<Message> Messages { get; set; } = [];

    public Paging? Paging { get; set; }
}