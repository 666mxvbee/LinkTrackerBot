using LinkTracker.Shared.Models;
using Npgsql;
using NpgsqlTypes;

namespace LinkTracker.Scrapper.Repositories.Sql;

public class SqlLinkRepository(NpgsqlDataSource dataSource) : ILinkRepository
{
    public void AddChat(long chatId)
    {
        using var command = dataSource.CreateCommand($"INSERT INTO chats (id) VALUES (@chatId) ON CONFLICT DO NOTHING;");
        
        command.Parameters.AddWithValue("chatId", chatId);
        command.ExecuteNonQuery();
    }

    public void RemoveChat(long chatId)
    {
        using var command = dataSource.CreateCommand($"DELETE FROM chats WHERE id = @chatId;");
        
        command.Parameters.AddWithValue("chatId", chatId);
        command.ExecuteNonQuery();
    }

    public bool ChatExists(long chatId)
    {
        using var command = dataSource.CreateCommand($"SELECT EXISTS (SELECT 1 FROM chats WHERE id = @chatId);");
        
        command.Parameters.AddWithValue("chatId", chatId);
        
        var result = command.ExecuteScalar();
        return result is bool exists && exists;
    }
    
       public LinkResponse? AddLink(long chatId, string url, string[]? tags)
    {
        var normalizedTags = NormalizeTags(tags);

        using var connection = dataSource.OpenConnection();
        using var transaction = connection.BeginTransaction();

        if (!ChatExists(connection, transaction, chatId))
        {
            return null;
        }

        var linkId = GetOrCreateLinkId(connection, transaction, url);

        var subscriptionCreated = AddChatLink(connection, transaction, chatId, linkId);
        if (!subscriptionCreated)
        {
            return null;
        }

        foreach (var tag in normalizedTags)
        {
            var tagId = GetOrCreateTagId(connection, transaction, tag);
            AddChatLinkTag(connection, transaction, chatId, linkId, tagId);
        }

        transaction.Commit();

        return new LinkResponse(linkId, url, normalizedTags);
    }

    public bool RemoveLink(long chatId, string url)
    {
        using var command = dataSource.CreateCommand("""
            DELETE FROM chat_links
            USING links
            WHERE chat_links.link_id = links.id
              AND chat_links.chat_id = @chatId
              AND links.url = @url;
            """);

        command.Parameters.AddWithValue("chatId", chatId);
        command.Parameters.AddWithValue("url", url);

        return command.ExecuteNonQuery() > 0;
    }

    public IEnumerable<LinkResponse> GetLinks(long chatId, string? tag = null, int offset = 0, int limit = 100)
    {
        using var command = dataSource.CreateCommand("""
            SELECT
                links.id,
                links.url,
                COALESCE(
                    array_agg(tags.name ORDER BY tags.name) FILTER (WHERE tags.name IS NOT NULL),
                    ARRAY[]::text[]
                ) AS tag_names
            FROM chat_links
            JOIN links ON links.id = chat_links.link_id
            LEFT JOIN chat_link_tags
                ON chat_link_tags.chat_id = chat_links.chat_id
               AND chat_link_tags.link_id = chat_links.link_id
            LEFT JOIN tags ON tags.id = chat_link_tags.tag_id
            WHERE chat_links.chat_id = @chatId
              AND (
                  @tag IS NULL
                  OR EXISTS (
                      SELECT 1
                      FROM chat_link_tags filter_chat_link_tags
                      JOIN tags filter_tags ON filter_tags.id = filter_chat_link_tags.tag_id
                      WHERE filter_chat_link_tags.chat_id = chat_links.chat_id
                        AND filter_chat_link_tags.link_id = chat_links.link_id
                        AND lower(filter_tags.name) = lower(@tag)
                  )
              )
            GROUP BY links.id, links.url, chat_links.created_at
            ORDER BY chat_links.created_at, links.id
            LIMIT @limit OFFSET @offset;
            """);

        command.Parameters.AddWithValue("chatId", chatId);
        command.Parameters.Add("tag", NpgsqlDbType.Text).Value =
            string.IsNullOrWhiteSpace(tag) ? DBNull.Value : tag.Trim();

        AddPaginationParameters(command, offset, limit);

        using var reader = command.ExecuteReader();
        var links = new List<LinkResponse>();

        while (reader.Read())
        {
            links.Add(new LinkResponse(
                reader.GetInt64(0),
                reader.GetString(1),
                reader.GetFieldValue<string[]>(2)));
        }

        return links;
    }

    public IEnumerable<(string Url, long[] ChatIds, DateTimeOffset LastUpdate)> GetLinksForUpdate(
        int offset = 0,
        int limit = 100)
    {
        using var command = dataSource.CreateCommand("""
            SELECT
                links.url,
                array_agg(chat_links.chat_id ORDER BY chat_links.chat_id) AS chat_ids,
                links.last_checked_at
            FROM links
            JOIN chat_links ON chat_links.link_id = links.id
            GROUP BY links.id, links.url, links.last_checked_at
            ORDER BY links.id
            LIMIT @limit OFFSET @offset;
            """);

        AddPaginationParameters(command, offset, limit);

        using var reader = command.ExecuteReader();
        var links = new List<(string Url, long[] ChatIds, DateTimeOffset LastUpdate)>();

        while (reader.Read())
        {
            links.Add((
                reader.GetString(0),
                reader.GetFieldValue<long[]>(1),
                reader.GetFieldValue<DateTimeOffset>(2)));
        }

        return links;
    }

    public void UpdateLastCheckTime(string url, DateTimeOffset lastUpdate)
    {
        using var command = dataSource.CreateCommand("""
            UPDATE links
            SET last_checked_at = @lastUpdate
            WHERE url = @url;
            """);

        command.Parameters.AddWithValue("url", url);
        command.Parameters.AddWithValue("lastUpdate", lastUpdate.ToUniversalTime());
        command.ExecuteNonQuery();
    }

    private static bool ChatExists(NpgsqlConnection connection, NpgsqlTransaction transaction, long chatId)
    {
        using var command = new NpgsqlCommand("""
            SELECT EXISTS (
                SELECT 1
                FROM chats
                WHERE id = @chatId
            );
            """, connection, transaction);

        command.Parameters.AddWithValue("chatId", chatId);

        var result = command.ExecuteScalar();
        return result is bool exists && exists;
    }

    private static long GetOrCreateLinkId(NpgsqlConnection connection, NpgsqlTransaction transaction, string url)
    {
        using var command = new NpgsqlCommand("""
            WITH inserted AS (
                INSERT INTO links (url)
                VALUES (@url)
                ON CONFLICT (url) DO NOTHING
                RETURNING id
            )
            SELECT id FROM inserted
            UNION ALL
            SELECT id FROM links WHERE url = @url
            LIMIT 1;
            """, connection, transaction);

        command.Parameters.AddWithValue("url", url);

        var result = command.ExecuteScalar() 
                     ?? throw new InvalidOperationException($"Unable to find link with url {url}");
        return (long)result;
    }

    private static bool AddChatLink(
        NpgsqlConnection connection,
        NpgsqlTransaction transaction,
        long chatId,
        long linkId)
    {
        using var command = new NpgsqlCommand("""
            INSERT INTO chat_links (chat_id, link_id)
            VALUES (@chatId, @linkId)
            ON CONFLICT DO NOTHING;
            """, connection, transaction);

        command.Parameters.AddWithValue("chatId", chatId);
        command.Parameters.AddWithValue("linkId", linkId);

        return command.ExecuteNonQuery() > 0;
    }

    private static long GetOrCreateTagId(NpgsqlConnection connection, NpgsqlTransaction transaction, string name)
    {
        using var command = new NpgsqlCommand("""
            WITH inserted AS (
                INSERT INTO tags (name)
                VALUES (@name)
                ON CONFLICT (name) DO NOTHING
                RETURNING id
            )
            SELECT id FROM inserted
            UNION ALL
            SELECT id FROM tags WHERE name = @name
            LIMIT 1;
            """, connection, transaction);

        command.Parameters.AddWithValue("name", name);

        var result = command.ExecuteScalar() 
                     ?? throw new InvalidOperationException($"Tag id was not returned from db");
        return (long)result;
    }

    private static void AddChatLinkTag(
        NpgsqlConnection connection,
        NpgsqlTransaction transaction,
        long chatId,
        long linkId,
        long tagId)
    {
        using var command = new NpgsqlCommand("""
            INSERT INTO chat_link_tags (chat_id, link_id, tag_id)
            VALUES (@chatId, @linkId, @tagId)
            ON CONFLICT DO NOTHING;
            """, connection, transaction);

        command.Parameters.AddWithValue("chatId", chatId);
        command.Parameters.AddWithValue("linkId", linkId);
        command.Parameters.AddWithValue("tagId", tagId);

        command.ExecuteNonQuery();
    }

    private static string[] NormalizeTags(string[]? tags)
    {
        return tags?
            .Where(tag => !string.IsNullOrWhiteSpace(tag))
            .Select(tag => tag.Trim())
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray() ?? Array.Empty<string>();
    }

    private static void AddPaginationParameters(NpgsqlCommand command, int offset, int limit)
    {
        command.Parameters.AddWithValue("offset", Math.Max(0, offset));
        command.Parameters.AddWithValue("limit", Math.Clamp(limit, 1, 1000));
    }
}