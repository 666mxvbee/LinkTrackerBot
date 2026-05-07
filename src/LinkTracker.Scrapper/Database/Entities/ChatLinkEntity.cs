namespace LinkTracker.Scrapper.Database.Entities;

public class ChatLinkEntity
{
    public long ChatId { get; set; }

    public long LinkId { get; set; }

    public DateTimeOffset CreatedAt { get; set; }

    public ChatEntity Chat { get; set; } = null!;

    public LinkEntity Link { get; set; } = null!;

    public ICollection<ChatLinkTagEntity> ChatLinkTags { get; set; } = new List<ChatLinkTagEntity>();
}