namespace LinkTracker.Scrapper.Database.Entities;

public class ChatLinkTagEntity
{
    public long ChatId { get; set; }

    public long LinkId { get; set; }

    public long TagId { get; set; }

    public ChatLinkEntity ChatLink { get; set; } = null!;

    public TagEntity Tag { get; set; } = null!;
}