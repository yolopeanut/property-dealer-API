using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using PropertyDealer.API.Tests;

public abstract class IntegrationTestBase : IClassFixture<TestWebApplicationFactory>, IDisposable
{
    protected readonly TestWebApplicationFactory Factory;
    protected readonly IServiceProvider ServiceProvider;
    protected readonly IServiceScope Scope;

    protected IntegrationTestBase(TestWebApplicationFactory factory)
    {
        Factory = factory;
        Scope = factory.Services.CreateScope();
        ServiceProvider = Scope.ServiceProvider;
    }

    public virtual void Dispose()
    {
        Scope?.Dispose();
    }
}