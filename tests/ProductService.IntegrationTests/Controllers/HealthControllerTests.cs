using System.Net;
using FluentAssertions;
using ProductService.IntegrationTests.Fixtures;

namespace ProductService.IntegrationTests.Controllers;

public sealed class HealthControllerTests(ProductsWebApplicationFactory factory)
    : IClassFixture<ProductsWebApplicationFactory>
{
    [Fact]
    public async Task Get_ReturnsOk_WithoutAuthentication()
    {
        var client = factory.CreateClient();

        var response = await client.GetAsync("/api/health");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task Get_ReturnsHealthyStatus()
    {
        var client = factory.CreateClient();

        var response = await client.GetAsync("/api/health");
        var body = await response.Content.ReadAsStringAsync();

        body.Should().Contain("Healthy");
    }
}
