using ApiSecurity.Domain.Entities;

namespace ApiSecurity.Application.Interfaces;

public interface IProductRepository
{
    Task<List<Product>> ListAsync(CancellationToken ct = default);
    Task AddAsync(Product product, CancellationToken ct = default);
    Task<Product?> FindAsync(Guid id, CancellationToken ct = default);
    Task RemoveAsync(Product product, CancellationToken ct = default);
    Task SaveChangesAsync(CancellationToken ct = default);
}
