namespace LinkTracker.Scrapper.Database.Entities;

public class TagEntity
{
    public long Id { get; set; }

    public string Name { get; set; } = string.Empty;

    public DateTimeOffset CreatedAt { get; set; }

    public ICollection<ChatLinkTagEntity> ChatLinkTags { get; set; } = new List<ChatLinkTagEntity>();
}