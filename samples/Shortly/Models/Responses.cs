namespace Shortly.Models;

public sealed record LinkResponse(string Slug, string Url, string ShortUrl, DateTimeOffset CreatedAt, int ExpiresInSeconds)
{
    public static LinkResponse From(Link link, string baseUrl) =>
        new(link.Slug, link.Url, $"{baseUrl}/r/{link.Slug}", link.CreatedAt, link.TtlSeconds);
}

public sealed record LinkStatsResponse(string Slug, string Url, long Clicks, int ExpiresInSeconds);
