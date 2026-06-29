using ApiSecurity.Application.Interfaces;
using ApiSecurity.Domain.Entities;
using ApiSecurity.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace ApiSecurity.Infrastructure.Repositories;

public class ApiKeyRepository(AppDbContext db) : IApiKeyRepository
{
    public Task<ApiKey?> FindByPrefixAsync(string prefix, CancellationToken ct = default)
        => db.ApiKeys.FirstOrDefaultAsync(k => k.KeyPrefix == prefix, ct);

    public Task<ApiKey?> FindByIdAsync(Guid id, CancellationToken ct = default)
        => db.ApiKeys.FindAsync([id], ct).AsTask();

    public async Task AddAsync(ApiKey apiKey, CancellationToken ct = default)
        => await db.ApiKeys.AddAsync(apiKey, ct);

    public Task SaveChangesAsync(CancellationToken ct = default)
        => db.SaveChangesAsync(ct);
}
