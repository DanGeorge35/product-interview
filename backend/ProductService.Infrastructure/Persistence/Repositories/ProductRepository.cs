using Microsoft.EntityFrameworkCore;
using ProductService.Domain.Entities;
using ProductService.Domain.Repositories;

namespace ProductService.Infrastructure.Persistence.Repositories;

public sealed class ProductRepository(ApplicationDbContext context) : IProductRepository
{
    public Task<Product?> GetByIdAsync(Guid id, CancellationToken ct = default) =>
        context.Products.FirstOrDefaultAsync(p => p.Id == id, ct);

    public async Task<IReadOnlyList<Product>> GetAllAsync(CancellationToken ct = default) =>
        await context.Products
            .AsNoTracking()
            .OrderByDescending(p => p.CreatedAt)
            .ToListAsync(ct);

    public async Task<IReadOnlyList<Product>> GetByColourAsync(Colour colour, CancellationToken ct = default) =>
        await context.Products
            .AsNoTracking()
            .Where(p => p.Colour == colour)
            .OrderByDescending(p => p.CreatedAt)
            .ToListAsync(ct);

    public async Task AddAsync(Product product, CancellationToken ct = default) =>
        await context.Products.AddAsync(product, ct);

    public Task<bool> ExistsAsync(Guid id, CancellationToken ct = default) =>
        context.Products.AnyAsync(p => p.Id == id, ct);
}
