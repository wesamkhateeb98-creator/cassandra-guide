using System.ComponentModel.DataAnnotations;

namespace Shortly.Models;

public sealed record CreateLinkRequest(
    Guid UserId,
    [Required, Url] string Url,
    [Range(0, 365)] int TtlDays = 0);

public sealed record UpdateLinkRequest([Required, Url] string Url);
