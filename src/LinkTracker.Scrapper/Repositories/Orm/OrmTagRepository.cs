using LinkTracker.Scrapper.Database;
using LinkTracker.Scrapper.Database.Entities;
using LinkTracker.Shared.Models;
using Microsoft.EntityFrameworkCore;

namespace LinkTracker.Scrapper.Repositories.Orm;

public class OrmTagRepository(LinkTrackerDbContext dbContext) : ITagRepository
{
    public TagResponse Create(string name)
    {
        var normalizedName = name.Trim();

        var existingTag = dbContext.Tags
            .AsNoTracking()
            .FirstOrDefault(tag => tag.Name == normalizedName);

        if (existingTag is not null)
        {
            return ToResponse(existingTag);
        }

        var tag = new TagEntity
        {
            Name = normalizedName
        };

        dbContext.Tags.Add(tag);
        dbContext.SaveChanges();

        return ToResponse(tag);
    }

    public TagResponse? Get(long id)
    {
        return dbContext.Tags
            .AsNoTracking()
            .Where(tag => tag.Id == id)
            .Select(tag => new TagResponse(tag.Id, tag.Name))
            .FirstOrDefault();
    }

    public IEnumerable<TagResponse> GetAll(int offset = 0, int limit = 100)
    {
        return dbContext.Tags
            .AsNoTracking()
            .OrderBy(tag => tag.Id)
            .Skip(NormalizeOffset(offset))
            .Take(NormalizeLimit(limit))
            .Select(tag => new TagResponse(tag.Id, tag.Name))
            .ToList();
    }

    public TagResponse? Update(long id, string name)
    {
        var tag = dbContext.Tags.FirstOrDefault(tag => tag.Id == id);

        if (tag is null)
        {
            return null;
        }

        tag.Name = name.Trim();
        dbContext.SaveChanges();

        return ToResponse(tag);
    }

    public bool Delete(long id)
    {
        var tag = dbContext.Tags.FirstOrDefault(tag => tag.Id == id);

        if (tag is null)
        {
            return false;
        }

        dbContext.Tags.Remove(tag);
        return dbContext.SaveChanges() > 0;
    }

    private static TagResponse ToResponse(TagEntity tag)
    {
        return new TagResponse(tag.Id, tag.Name);
    }

    private static int NormalizeOffset(int offset)
    {
        return Math.Max(0, offset);
    }

    private static int NormalizeLimit(int limit)
    {
        return Math.Clamp(limit, 1, 1000);
    }
}