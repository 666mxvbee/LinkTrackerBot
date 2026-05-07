namespace LinkTracker.Scrapper.Database.Entities;

public class ChatEntity
{
    public long Id { get; set; }

    public DateTimeOffset CreatedAt { get; set; }

    public ICollection<ChatLinkEntity> ChatLinks { get; set; } = new List<ChatLinkEntity>();
}