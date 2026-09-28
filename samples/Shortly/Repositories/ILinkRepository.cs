using Shortly.Models;

namespace Shortly.Repositories;

public interface ILinkRepository
{
    Task CreateAsync(Link link);
    Task<Link?> GetAsync(string slug);
    Task<IReadOnlyList<Link>> GetByUserAsync(Guid userId, int limit);
    Task UpdateUrlAsync(Link link, string newUrl);
    Task DeleteAsync(Link link);
}
