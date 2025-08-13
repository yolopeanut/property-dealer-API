using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace PropertyDealer.API.Tests;

public class TestWebApplicationFactory : WebApplicationFactory<Program>
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureServices(services =>
        {
            // Override any services for testing if needed
            // For example, you might want to use in-memory database or mock external services

            // Remove the existing logging configuration and add test logging
            services.AddLogging(builder => builder.AddConsole().SetMinimumLevel(LogLevel.Debug));

            // You can override specific services here for testing
            // services.Replace(ServiceDescriptor.Singleton<ISomeService, MockSomeService>());
        });

        // Use test environment
        builder.UseEnvironment("Testing");
    }
}