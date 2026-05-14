using LinkTracker.Shared.Models;

namespace LinkTracker.Scrapper.Repositories;

public interface ITagRepository
{
    TagResponse Create(string name);
    TagResponse? Get(long Id);
    IEnumerable<TagResponse> GetAll(int offset = 0, int limit = 100);
    TagResponse? Update(long id, string name);
    bool Delete(long id);
}