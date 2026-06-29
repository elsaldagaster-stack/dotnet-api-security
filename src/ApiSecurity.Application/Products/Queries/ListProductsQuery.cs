using ApiSecurity.Application.Interfaces;
using ApiSecurity.Domain.Entities;
using MediatR;

namespace ApiSecurity.Application.Products.Queries;

public record ListProductsQuery : IRequest<List<Product>>;

public class ListProductsQueryHandler(IProductRepository repository)
    : IRequestHandler<ListProductsQuery, List<Product>>
{
    public Task<List<Product>> Handle(ListProductsQuery request, CancellationToken ct)
        => repository.ListAsync(ct);
}
