namespace LinkTracker.Scrapper.Database.Entities;

public class LinkEntity
{
    public long Id { get; set; }

    public string Url { get; set; } = string.Empty;

    public DateTimeOffset LastCheckedAt { get; set; }

    public DateTimeOffset CreatedAt { get; set; }

    public ICollection<ChatLinkEntity> ChatLinks { get; set; } = new List<ChatLinkEntity>();
}