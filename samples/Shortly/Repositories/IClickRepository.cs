namespace Shortly.Repositories;

public interface IClickRepository
{
    Task IncrementAsync(string slug);
    Task<long> GetAsync(string slug);
    Task DeleteAsync(string slug);
}
