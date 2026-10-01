using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using ProductService.Application.Products.DTOs;
using ProductService.IntegrationTests.Fixtures;

namespace ProductService.IntegrationTests.Controllers;

public sealed class ProductsControllerTests(ProductsWebApplicationFactory factory)
    : IClassFixture<ProductsWebApplicationFactory>
{
    // ── Authentication ─────────────────────────────────────────────────────────

    [Fact]
    public async Task GetAll_WithoutToken_Returns401()
    {
        var client = factory.CreateClient();

        var response = await client.GetAsync("/api/products");

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Create_WithoutToken_Returns401()
    {
        var client = factory.CreateClient();

        var response = await client.PostAsJsonAsync("/api/products",
            new { name = "Test", colour = "Red", price = 10.0m, stockQuantity = 5 });

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    // ── Create ─────────────────────────────────────────────────────────────────

    [Fact]
    public async Task Create_ValidProduct_Returns201WithLocationHeader()
    {
        var client = factory.CreateAuthenticatedClient();

        var response = await client.PostAsJsonAsync("/api/products", new
        {
            name = "Classic Red Shirt",
            description = "A timeless red shirt.",
            colour = "Red",
            price = 29.99m,
            stockQuantity = 150
        });

        response.StatusCode.Should().Be(HttpStatusCode.Created);
        response.Headers.Location.Should().NotBeNull();

        var product = await response.Content.ReadFromJsonAsync<ProductDto>();
        product.Should().NotBeNull();
        product!.Name.Should().Be("Classic Red Shirt");
        product.Colour.Should().Be("Red");
        product.Price.Should().Be(29.99m);
        product.StockQuantity.Should().Be(150);
        product.Id.Should().NotBeEmpty();
    }

    [Fact]
    public async Task Create_WithNegativePrice_Returns422WithErrors()
    {
        var client = factory.CreateAuthenticatedClient();

        var response = await client.PostAsJsonAsync("/api/products", new
        {
            name = "Bad Product",
            colour = "Blue",
            price = -1.0m,
            stockQuantity = 5
        });

        response.StatusCode.Should().Be(HttpStatusCode.UnprocessableEntity);

        var body = await response.Content.ReadAsStringAsync();
        body.Should().Contain("positive");
    }

    [Fact]
    public async Task Create_WithInvalidColour_Returns422WithErrors()
    {
        var client = factory.CreateAuthenticatedClient();

        var response = await client.PostAsJsonAsync("/api/products", new
        {
            name = "Bad Product",
            colour = "Chartreuse",
            price = 10m,
            stockQuantity = 5
        });

        response.StatusCode.Should().Be(HttpStatusCode.UnprocessableEntity);
    }

    [Fact]
    public async Task Create_WithEmptyName_Returns422WithErrors()
    {
        var client = factory.CreateAuthenticatedClient();

        var response = await client.PostAsJsonAsync("/api/products", new
        {
            name = "",
            colour = "Red",
            price = 10m,
            stockQuantity = 5
        });

        response.StatusCode.Should().Be(HttpStatusCode.UnprocessableEntity);
    }

    // ── GetAll ─────────────────────────────────────────────────────────────────

    [Fact]
    public async Task GetAll_WithToken_Returns200WithProductList()
    {
        var client = factory.CreateAuthenticatedClient();
        await client.PostAsJsonAsync("/api/products",
            new { name = "Product A", colour = "Red", price = 10m, stockQuantity = 5 });

        var response = await client.GetAsync("/api/products");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var products = await response.Content.ReadFromJsonAsync<List<ProductDto>>();
        products.Should().NotBeNull();
        products!.Should().NotBeEmpty();
    }

    // ── GetById ────────────────────────────────────────────────────────────────

    [Fact]
    public async Task GetById_ExistingProduct_Returns200()
    {
        var client = factory.CreateAuthenticatedClient();
        var createResponse = await client.PostAsJsonAsync("/api/products",
            new { name = "Lookup Product", colour = "Green", price = 15m, stockQuantity = 10 });
        var created = await createResponse.Content.ReadFromJsonAsync<ProductDto>();

        var response = await client.GetAsync($"/api/products/{created!.Id}");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var product = await response.Content.ReadFromJsonAsync<ProductDto>();
        product!.Id.Should().Be(created.Id);
        product.Name.Should().Be("Lookup Product");
    }

    [Fact]
    public async Task GetById_NonExistentProduct_Returns404()
    {
        var client = factory.CreateAuthenticatedClient();

        var response = await client.GetAsync($"/api/products/{Guid.NewGuid()}");

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    // ── GetByColour ────────────────────────────────────────────────────────────

    [Fact]
    public async Task GetByColour_ValidColour_ReturnsFilteredProducts()
    {
        var client = factory.CreateAuthenticatedClient();
        await client.PostAsJsonAsync("/api/products",
            new { name = "Red Item 1", colour = "Red", price = 10m, stockQuantity = 5 });
        await client.PostAsJsonAsync("/api/products",
            new { name = "Blue Item 1", colour = "Blue", price = 20m, stockQuantity = 3 });

        var response = await client.GetAsync("/api/products/by-colour/Red");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var products = await response.Content.ReadFromJsonAsync<List<ProductDto>>();
        products.Should().NotBeNull();
        products!.Should().AllSatisfy(p => p.Colour.Should().Be("Red"));
    }

    [Fact]
    public async Task GetByColour_CaseInsensitive_ReturnsFilteredProducts()
    {
        var client = factory.CreateAuthenticatedClient();
        await client.PostAsJsonAsync("/api/products",
            new { name = "Blue Widget", colour = "Blue", price = 10m, stockQuantity = 5 });

        var response = await client.GetAsync("/api/products/by-colour/blue");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var products = await response.Content.ReadFromJsonAsync<List<ProductDto>>();
        products.Should().NotBeNull();
        products!.Should().AllSatisfy(p => p.Colour.Should().Be("Blue"));
    }

    [Fact]
    public async Task GetByColour_InvalidColour_Returns400WithDescriptiveError()
    {
        var client = factory.CreateAuthenticatedClient();

        var response = await client.GetAsync("/api/products/by-colour/Chartreuse");

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        var body = await response.Content.ReadAsStringAsync();
        body.Should().Contain("Chartreuse");
    }
}
