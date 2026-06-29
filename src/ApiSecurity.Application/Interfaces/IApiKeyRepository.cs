using ApiSecurity.Domain.Entities;

namespace ApiSecurity.Application.Interfaces;

public interface IApiKeyRepository
{
    Task<ApiKey?> FindByPrefixAsync(string prefix, CancellationToken ct = default);
    Task<ApiKey?> FindByIdAsync(Guid id, CancellationToken ct = default);
    Task AddAsync(ApiKey apiKey, CancellationToken ct = default);
    Task SaveChangesAsync(CancellationToken ct = default);
}
