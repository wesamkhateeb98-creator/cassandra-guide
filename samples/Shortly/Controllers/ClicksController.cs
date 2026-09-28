using Microsoft.AspNetCore.Mvc;
using Shortly.Models;
using Shortly.Repositories;

namespace Shortly.Controllers;

[ApiController]
public sealed class ClicksController(ILinkRepository links, IClickRepository clicks) : ControllerBase
{
    // SELECT by partition key + counter UPDATE, then 302
    [HttpGet("r/{slug}")]
    public async Task<IActionResult> Follow(string slug)
    {
        var link = await links.GetAsync(slug);
        if (link is null)
            return NotFound(); // expired by TTL or deleted

        await clicks.IncrementAsync(slug);
        return Redirect(link.Url);
    }

    // SELECT counter + TTL(url)
    [HttpGet("api/links/{slug}/stats")]
    public async Task<ActionResult<LinkStatsResponse>> Stats(string slug)
    {
        var link = await links.GetAsync(slug);
        if (link is null)
            return NotFound();

        var count = await clicks.GetAsync(slug);
        return new LinkStatsResponse(slug, link.Url, count, link.TtlSeconds);
    }
}
