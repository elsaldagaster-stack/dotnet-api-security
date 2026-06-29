using ApiSecurity.Application.Interfaces;
using ApiSecurity.Domain.Entities;
using FluentValidation;
using MediatR;

namespace ApiSecurity.Application.Products.Commands;

public record CreateProductCommand(string Name, decimal Price, int Stock) : IRequest<Guid>;

public class CreateProductCommandHandler(IProductRepository repository)
    : IRequestHandler<CreateProductCommand, Guid>
{
    public async Task<Guid> Handle(CreateProductCommand request, CancellationToken ct)
    {
        var product = Product.Create(request.Name, request.Price, request.Stock);
        await repository.AddAsync(product, ct);
        await repository.SaveChangesAsync(ct);
        return product.Id;
    }
}

public class CreateProductCommandValidator : AbstractValidator<CreateProductCommand>
{
    public CreateProductCommandValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(200);
        RuleFor(x => x.Price).GreaterThan(0);
        RuleFor(x => x.Stock).GreaterThanOrEqualTo(0);
    }
}
