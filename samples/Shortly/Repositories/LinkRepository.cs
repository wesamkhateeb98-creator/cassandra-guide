using Cassandra;
using ISession = Cassandra.ISession;
using Shortly.Models;

namespace Shortly.Repositories;

/// <summary>
/// One link lives in two tables (links_by_slug + links_by_user).
/// Every write touches both inside a LOGGED batch, so they never drift apart.
/// </summary>
public sealed class LinkRepository : ILinkRepository
{
    private readonly ISession _session;
    private readonly PreparedStatement _insertBySlug;
    private readonly PreparedStatement _insertByUser;
    private readonly PreparedStatement _selectBySlug;
    private readonly PreparedStatement _selectByUser;
    private readonly PreparedStatement _updateBySlug;
    private readonly PreparedStatement _updateByUser;
    private readonly PreparedStatement _deleteBySlug;
    private readonly PreparedStatement _deleteByUser;

    private LinkRepository(ISession session, PreparedStatement[] ps)
    {
        _session = session;
        (_insertBySlug, _insertByUser, _selectBySlug, _selectByUser,
         _updateBySlug, _updateByUser, _deleteBySlug, _deleteByUser) =
            (ps[0], ps[1], ps[2], ps[3], ps[4], ps[5], ps[6], ps[7]);
    }

    // Prepare once at startup, reuse for every request.
    public static async Task<LinkRepository> CreateAsync(ISession session)
    {
        var ps = await Task.WhenAll(
            // INSERT + TTL (0 = never expires)
            session.PrepareAsync(
                "INSERT INTO shortly.links_by_slug (slug, url, user_id, created_at) VALUES (?, ?, ?, ?) USING TTL ?"),
            session.PrepareAsync(
                "INSERT INTO shortly.links_by_user (user_id, created_at, slug, url) VALUES (?, ?, ?, ?) USING TTL ?"),

            // SELECT — TTL(col) returns the seconds left, null when the column has no TTL
            session.PrepareAsync(
                "SELECT slug, url, user_id, created_at, TTL(url) AS ttl FROM shortly.links_by_slug WHERE slug = ?"),
            session.PrepareAsync(
                "SELECT slug, url, user_id, created_at, TTL(url) AS ttl FROM shortly.links_by_user WHERE user_id = ? LIMIT ?"),

            // UPDATE — the full primary key is required in WHERE
            session.PrepareAsync(
                "UPDATE shortly.links_by_slug USING TTL ? SET url = ? WHERE slug = ?"),
            session.PrepareAsync(
                "UPDATE shortly.links_by_user USING TTL ? SET url = ? WHERE user_id = ? AND created_at = ? AND slug = ?"),

            // DELETE — writes a tombstone, not an in-place removal
            session.PrepareAsync(
                "DELETE FROM shortly.links_by_slug WHERE slug = ?"),
            session.PrepareAsync(
                "DELETE FROM shortly.links_by_user WHERE user_id = ? AND created_at = ? AND slug = ?"));
        return new LinkRepository(session, ps);
    }

    public Task CreateAsync(Link link)
    {
        var batch = new BatchStatement() // BatchType.Logged by default
            .Add(_insertBySlug.Bind(link.Slug, link.Url, link.UserId, link.CreatedAt, link.TtlSeconds))
            .Add(_insertByUser.Bind(link.UserId, link.CreatedAt, link.Slug, link.Url, link.TtlSeconds));
        return _session.ExecuteAsync(batch.SetIdempotence(true));
    }

    public async Task<Link?> GetAsync(string slug)
    {
        var rs = await _session.ExecuteAsync(_selectBySlug.Bind(slug).SetIdempotence(true));
        var row = rs.FirstOrDefault();
        return row is null ? null : ToLink(row);
    }

    public async Task<IReadOnlyList<Link>> GetByUserAsync(Guid userId, int limit)
    {
        var rs = await _session.ExecuteAsync(_selectByUser.Bind(userId, limit).SetIdempotence(true));
        return rs.Select(ToLink).ToList();
    }

    public Task UpdateUrlAsync(Link link, string newUrl)
    {
        // UPDATE without USING TTL would store url with NO TTL: the row would outlive its expiry.
        // Re-apply the seconds left so url dies together with the rest of the row.
        var batch = new BatchStatement()
            .Add(_updateBySlug.Bind(link.TtlSeconds, newUrl, link.Slug))
            .Add(_updateByUser.Bind(link.TtlSeconds, newUrl, link.UserId, link.CreatedAt, link.Slug));
        return _session.ExecuteAsync(batch.SetIdempotence(true));
    }

    public Task DeleteAsync(Link link)
    {
        var batch = new BatchStatement()
            .Add(_deleteBySlug.Bind(link.Slug))
            .Add(_deleteByUser.Bind(link.UserId, link.CreatedAt, link.Slug));
        return _session.ExecuteAsync(batch.SetIdempotence(true));
    }

    private static Link ToLink(Row row) => new(
        row.GetValue<string>("slug"),
        row.GetValue<string>("url"),
        row.GetValue<Guid>("user_id"),
        row.GetValue<DateTimeOffset>("created_at"),
        row.GetValue<int?>("ttl") ?? 0);
}
