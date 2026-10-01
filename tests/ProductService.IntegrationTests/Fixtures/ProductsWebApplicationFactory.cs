using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using ProductService.Application.Common.Interfaces;
using ProductService.Infrastructure.Persistence;

namespace ProductService.IntegrationTests.Fixtures;

public sealed class ProductsWebApplicationFactory : WebApplicationFactory<Program>
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");

        builder.ConfigureServices(services =>
        {
            // Remove all existing DbContext registrations
            var toRemove = services
                .Where(d => d.ServiceType == typeof(DbContextOptions<ApplicationDbContext>)
                         || d.ServiceType == typeof(ApplicationDbContext)
                         || d.ServiceType == typeof(IUnitOfWork))
                .ToList();

            foreach (var descriptor in toRemove)
                services.Remove(descriptor);

            var dbName = $"TestDb_{Guid.NewGuid()}";
            services.AddDbContext<ApplicationDbContext>(options =>
                options.UseInMemoryDatabase(dbName));

            services.AddScoped<IUnitOfWork>(sp =>
                sp.GetRequiredService<ApplicationDbContext>());
        });

        builder.ConfigureAppConfiguration((_, config) =>
        {
            var testSettings = new Dictionary<string, string?>
            {
                ["Jwt:Issuer"] = Helpers.JwtTokenHelper.TestIssuer,
                ["Jwt:Audience"] = Helpers.JwtTokenHelper.TestAudience,
                ["Jwt:SecretKey"] = Helpers.JwtTokenHelper.TestSecretKey,
                ["Jwt:ExpiryMinutes"] = "60",
                ["ConnectionStrings:DefaultConnection"] = "InMemory"
            };

            config.AddInMemoryCollection(testSettings);
        });
    }

    public HttpClient CreateAuthenticatedClient()
    {
        var client = CreateClient();
        var token = Helpers.JwtTokenHelper.GenerateToken();
        client.DefaultRequestHeaders.Authorization =
            new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);
        return client;
    }
}
