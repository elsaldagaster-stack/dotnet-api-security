using ApiSecurity.Application.Interfaces;
using MediatR;

namespace ApiSecurity.Application.Products.Commands;

public record DeleteProductCommand(Guid Id) : IRequest<bool>;

public class DeleteProductCommandHandler(IProductRepository repository)
    : IRequestHandler<DeleteProductCommand, bool>
{
    public async Task<bool> Handle(DeleteProductCommand request, CancellationToken ct)
    {
        var product = await repository.FindAsync(request.Id, ct);
        if (product is null) return false;
        await repository.RemoveAsync(product, ct);
        await repository.SaveChangesAsync(ct);
        return true;
    }
}
