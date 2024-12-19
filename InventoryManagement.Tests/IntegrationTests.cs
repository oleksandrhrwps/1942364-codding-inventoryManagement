using InventoryManagement.WebAPI.Data;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.VisualStudio.TestPlatform.TestHost;

namespace InventoryManagement.Tests
{
    public class IntegrationTests : IClassFixture<WebApplicationFactory<Program>>
    {
        private readonly WebApplicationFactory<Program> _factory;

        public HttpClient Client { get; private set; }

        public IntegrationTests(WebApplicationFactory<Program> factory)
        {
            _factory = factory;
            SetUpClient();
        }

        [Fact]
        public void Test1()
        {

        }

        private void SetUpClient()
        {
            Client = _factory.WithWebHostBuilder(builder =>
                builder.ConfigureServices(services =>
                {
                    // Remove existing DbContext registration
                    var descriptor = services.SingleOrDefault(
                        d => d.ServiceType == typeof(DbContextOptions<InventoryDbContext>));

                    if (descriptor != null) services.Remove(descriptor);

                    // Add a new DbContext for testing with in-memory SQLite database
                    services.AddDbContext<InventoryDbContext>(options =>
                    {
                        options.UseSqlite("Data Source=:memory:")
                               .EnableSensitiveDataLogging();
                    });

                    // Build a service provider to instantiate the context
                    var sp = services.BuildServiceProvider();

                    // Create a new scope to setup the database
                    using var scope = sp.CreateScope();                    
                    var scopedServices = scope.ServiceProvider;
                    var db = scopedServices.GetRequiredService<InventoryDbContext>();
                    db.Database.OpenConnection();
                    db.Database.EnsureCreated();

                    db.SaveChanges();

                    // Clear local context cache
                    foreach (var entity in db.ChangeTracker.Entries().ToList())
                    {
                        entity.State = EntityState.Detached;
                    }

                })).CreateClient();
        }
    }
}