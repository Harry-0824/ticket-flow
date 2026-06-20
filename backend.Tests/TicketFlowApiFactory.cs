using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.DependencyInjection;
using TicketFlow.Api.Data;

namespace TicketFlow.Api.Tests;

public sealed class TicketFlowApiFactory : WebApplicationFactory<Program>
{
    private readonly IInterceptor[] interceptors;
    private readonly SqliteConnection connection = new("DataSource=:memory:");

    public TicketFlowApiFactory()
        : this([])
    {
    }

    internal TicketFlowApiFactory(params IInterceptor[] interceptors)
    {
        this.interceptors = interceptors;
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        connection.Open();
        builder.UseEnvironment("Testing");

        builder.ConfigureServices(services =>
        {
            var descriptor = services.SingleOrDefault(service =>
                service.ServiceType == typeof(DbContextOptions<TicketFlowDbContext>));

            if (descriptor is not null)
            {
                services.Remove(descriptor);
            }

            services.AddDbContext<TicketFlowDbContext>(options =>
            {
                options.UseSqlite(connection);

                if (interceptors.Length > 0)
                {
                    options.AddInterceptors(interceptors);
                }
            });

            using var serviceProvider = services.BuildServiceProvider();
            using var scope = serviceProvider.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<TicketFlowDbContext>();
            db.Database.EnsureCreated();
        });
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);
        connection.Dispose();
    }
}
