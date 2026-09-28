using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc;
using Shortly.Models;
using Shortly.Repositories;

namespace Shortly.Controllers;

[ApiController]
[Route("api/links")]
public sealed class LinksController(ILinkRepository links, IClickRepository clicks) : ControllerBase
{
    private string BaseUrl => $"{Request.Scheme}://{Request.Host}";

    // BATCH + INSERT + TTL
    [HttpPost]
    public async Task<ActionResult<LinkResponse>> Create(CreateLinkRequest request)
    {
        var link = new Link(
            NewSlug(),
            request.Url,
            request.UserId,
            DateTimeOffset.FromUnixTimeMilliseconds(DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()), // Cassandra keeps ms
            (int)TimeSpan.FromDays(request.TtlDays).TotalSeconds);

        await links.CreateAsync(link);

        var response = LinkResponse.From(link, BaseUrl);
        return Created(response.ShortUrl, response);
    }

    // SELECT from one partition, already sorted by clustering key (newest first)
    [HttpGet]
    public async Task<IReadOnlyList<LinkResponse>> GetByUser([FromQuery] Guid userId, [FromQuery, Range(1, 100)] int limit = 20)
    {
        var userLinks = await links.GetByUserAsync(userId, limit);
        return userLinks.Select(link => LinkResponse.From(link, BaseUrl)).ToList();
    }

    // BATCH + UPDATE (keeps the remaining TTL)
    [HttpPut("{slug}")]
    public async Task<IActionResult> Update(string slug, UpdateLinkRequest request)
    {
        var link = await links.GetAsync(slug);
        if (link is null)
            return NotFound();

        await links.UpdateUrlAsync(link, request.Url);
        return NoContent();
    }

    // BATCH + DELETE, then the counter on its own (counters can't join a logged batch)
    [HttpDelete("{slug}")]
    public async Task<IActionResult> Delete(string slug)
    {
        var link = await links.GetAsync(slug);
        if (link is null)
            return NotFound();

        await links.DeleteAsync(link);
        await clicks.DeleteAsync(slug);
        return NoContent();
    }

    // 7 chars of base62 ≈ 3.5 trillion combinations; collisions are ignored in this demo.
    private static string NewSlug() =>
        Random.Shared.GetString("0123456789abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ", 7);
}
