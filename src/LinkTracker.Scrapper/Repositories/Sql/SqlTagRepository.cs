using LinkTracker.Shared.Models;
using Npgsql;

namespace LinkTracker.Scrapper.Repositories.Sql;

public class SqlTagRepository(NpgsqlDataSource dataSource) : ITagRepository
{
    public TagResponse Create(string name)
    {
        const string sql = 
            """
             WITH inserted AS (
                INSERT INTO tags (name)
                VALUES (@name)
                ON CONFLICT (name) DO NOTHING
                RETURNING id, name
            )
            SELECT id, name FROM inserted
            UNION ALL
            SELECT id, name FROM tags WHERE name = @name
            LIMIT 1;
            """;

        using var command = dataSource.CreateCommand(sql);
        command.Parameters.AddWithValue("name", name.Trim());
        
        using var reader = command.ExecuteReader();
        reader.Read();
        
        return new TagResponse(reader.GetInt64(0), reader.GetString(1));
    }

    public TagResponse? Get(long id)
    {
        using var command = dataSource.CreateCommand($"SELECT id, name FROM tags WHERE id = @id");
        
        command.Parameters.AddWithValue("id", id);
        
        using var reader = command.ExecuteReader();
        
        return reader.Read() ? new TagResponse(reader.GetInt64(0), reader.GetString(1)) : null;
    }

    public IEnumerable<TagResponse> GetAll(int offset = 0, int limit = 100)
    {
        using var command = dataSource.CreateCommand($"SELECT id, name FROM tags ORDER BY id LIMIT @limit OFFSET @offset");
        
        AddPaginationParameters(command, offset, limit);

        using var reader = command.ExecuteReader();
        var tags = new List<TagResponse>();

        while (reader.Read())
        {
            tags.Add(new TagResponse(reader.GetInt64(0), reader.GetString(1)));
        }

        return tags;
    }

    public TagResponse? Update(long id, string name)
    {
        using var command = dataSource.CreateCommand($"UPDATE tags SET name = @name WHERE id = @id RETURNING id, name");
        
        command.Parameters.AddWithValue("id", id);
        command.Parameters.AddWithValue("name", name.Trim());

        using var reader = command.ExecuteReader();
        
        return reader.Read() ? new TagResponse(reader.GetInt64(0),  reader.GetString(1)) : null;
    }

    public bool Delete(long id)
    {
        using var command = dataSource.CreateCommand($"DELETE FROM tags WHERE id = @id");
        
        command.Parameters.AddWithValue("id", id);
        
        return command.ExecuteNonQuery() > 0;
    }

    private static void AddPaginationParameters(NpgsqlCommand command, int offset, int limit)
    {
        command.Parameters.AddWithValue("offset", Math.Max(0, offset));
        command.Parameters.AddWithValue("limit", Math.Clamp(limit, 1, 1000));
    }
}