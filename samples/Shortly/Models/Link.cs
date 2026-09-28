namespace Shortly.Models;

/// <param name="TtlSeconds">On write: lifetime to set. On read: seconds left. 0 = never expires.</param>
public sealed record Link(string Slug, string Url, Guid UserId, DateTimeOffset CreatedAt, int TtlSeconds);
