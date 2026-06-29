using ApiSecurity.Application.Interfaces;
using ApiSecurity.Domain.Entities;
using ApiSecurity.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace ApiSecurity.Infrastructure.Repositories;

public class ProductRepository(AppDbContext db) : IProductRepository
{
    public Task<List<Product>> ListAsync(CancellationToken ct = default) => db.Products.ToListAsync(ct);

    public async Task AddAsync(Product product, CancellationToken ct = default) => await db.Products.AddAsync(product, ct);

    public Task<Product?> FindAsync(Guid id, CancellationToken ct = default) => db.Products.FindAsync([id], ct).AsTask();

    public Task RemoveAsync(Product product, CancellationToken ct = default)
    {
        db.Products.Remove(product);
        return Task.CompletedTask;
    }

    public Task SaveChangesAsync(CancellationToken ct = default) => db.SaveChangesAsync(ct);
}
