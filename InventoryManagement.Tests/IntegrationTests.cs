using InventoryManagement.WebAPI.Data;
using InventoryManagement.WebAPI.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.VisualStudio.TestPlatform.TestHost;
using System.Collections;
using System.Text;
using System.Text.Json;

namespace InventoryManagement.Tests
{
    public class IntegrationTests : IClassFixture<WebApplicationFactory<Program>>
    {
        private readonly WebApplicationFactory<Program> _factory;

        private const string ApiEndpointUploadCsv = "/api/inventory/upload-csv";
        private const string ApiEndpointVerifyItem = "/api/inventory/verify-item";
        private const string ApiEndpointDiscrepancies = "/api/inventory/discrepancies";

        public HttpClient Client { get; private set; }
        private InventoryDbContext _db;

        public IntegrationTests(WebApplicationFactory<Program> factory)
        {
            _factory = factory;
            SetUpClient();
        }

        [Fact]
        public async Task Test_UploadCsv_Returns200()
        {
            var stringContent = await GetStringContentAsync("ItemsCollection.csv");
            var response = await Client.PostAsync(ApiEndpointUploadCsv, stringContent);

            Assert.Equal(System.Net.HttpStatusCode.OK, response.StatusCode);
        }

        [Fact]
        public async Task Test_UploadCsv_InvalidDate_Returns400()
        {
            var inventoryItem = new InventoryItem
            {
                Barcode = Guid.NewGuid(),
                StorageLocationName = "Test Location",
                ItemDescription = "Test Description",
                CreatedDate = DateTime.Now.AddDays(30),
            };
            var stringContent = GetStringContent(inventoryItem);
            var response = await Client.PostAsync(ApiEndpointUploadCsv, stringContent);

            Assert.Equal(System.Net.HttpStatusCode.BadRequest, response.StatusCode);
        }

        [Fact]
        public async Task Test_UploadCsv_InvalidGuid_Returns400()
        {
            var inventoryItem = new InventoryItem
            {
                Barcode = Guid.Empty,
                StorageLocationName = "Test Location",
                ItemDescription = "Test Description",
                CreatedDate = DateTime.Now.AddDays(-30),
            };
            var stringContent = GetStringContent(inventoryItem);
            var response = await Client.PostAsync(ApiEndpointUploadCsv, stringContent);

            Assert.Equal(System.Net.HttpStatusCode.BadRequest, response.StatusCode);
        }

        [Fact]
        public async Task Test_UploadCsv_InvalidStorageLocationName_Returns400()
        {
            var inventoryItem = new InventoryItem
            {
                Barcode = Guid.NewGuid(),
                StorageLocationName = "",
                ItemDescription = "Test Description",
                CreatedDate = DateTime.Now.AddDays(-30),
            };
            var stringContent = GetStringContent(inventoryItem);
            var response = await Client.PostAsync(ApiEndpointUploadCsv, stringContent);

            Assert.Equal(System.Net.HttpStatusCode.BadRequest, response.StatusCode);
        }

        [Fact]
        public async Task Test_VerifyItem_Returns404()
        {
            var response =
                await Client.GetAsync($"{ApiEndpointVerifyItem}?barcode={Guid.NewGuid()}&currentLocation=SampleLocation");

            Assert.Equal(System.Net.HttpStatusCode.NotFound, response.StatusCode);
        }

        [Fact]
        public async Task Test_VerifyItem_Returns200()
        {
            var generatedGuid = Guid.NewGuid();
            var locationName = "Test Location";

            var inventoryItem = new InventoryItem
            {
                Barcode = generatedGuid,
                StorageLocationName = locationName,
                ItemDescription = "Test Description",
                CreatedDate = DateTime.Now.AddDays(-30),
            };
            var stringContent = GetStringContent(inventoryItem);
            var responsePrepareData = await Client.PostAsync(ApiEndpointUploadCsv, stringContent);

            Assert.Equal(System.Net.HttpStatusCode.OK, responsePrepareData.StatusCode);

            var response =
                await Client.GetAsync($"{ApiEndpointVerifyItem}?barcode={generatedGuid}&currentLocation={locationName}");

            Assert.Equal(System.Net.HttpStatusCode.OK, response.StatusCode);
        }

        [Fact]
        public async Task Test_VerifyItem_Returns400()
        {
            var generatedGuid = Guid.NewGuid();
            var expectedLocationName = "Expected Location";
            var actualLocationName = "Actual Location";

            var inventoryItem = new InventoryItem
            {
                Barcode = generatedGuid,
                StorageLocationName = actualLocationName,
                ItemDescription = "Test Description",
                CreatedDate = DateTime.Now.AddDays(-30),
            };
            var stringContent = GetStringContent(inventoryItem);
            var responsePrepareData = await Client.PostAsync(ApiEndpointUploadCsv, stringContent);

            Assert.Equal(System.Net.HttpStatusCode.OK, responsePrepareData.StatusCode);

            var response =
                await Client.GetAsync($"{ApiEndpointVerifyItem}?barcode={generatedGuid}&currentLocation={expectedLocationName}");

            Assert.Equal(System.Net.HttpStatusCode.BadRequest, response.StatusCode);
        }

        [Fact]
        public async Task Test_GetDiscrepancies_Returns200()
        {
            ResetDatabase();

            var testData = new List<DiscrepancyRecord>
            {
                new() { Barcode = Guid.NewGuid(), ScanningDate = DateTime.UtcNow, ActualStorageLocation = "Location1" },
                new() { Barcode = Guid.NewGuid(), ScanningDate = DateTime.UtcNow, ActualStorageLocation = "Location2" },
            };

            _db.DiscrepancyRecords.AddRange();

            await _db.SaveChangesAsync();

            var response = await Client.GetAsync(ApiEndpointDiscrepancies);

            Assert.Equal(System.Net.HttpStatusCode.OK, response.StatusCode);
            var content = await response.Content.ReadAsStringAsync();
            Assert.NotNull(content);
            var discrepancies = JsonSerializer.Deserialize<IEnumerable>(content);
            Assert.NotNull(discrepancies);

            Assert.IsAssignableFrom<IEnumerable>(discrepancies);
            Assert.Equal(testData.Count, discrepancies.Cast<object>().Count());
        }

        private static async Task<StringContent> GetStringContentAsync(string csvFilePath)
        {
            var csvData = await File.ReadAllTextAsync(csvFilePath);
            var csvBase64 = Convert.ToBase64String(Encoding.UTF8.GetBytes(csvData));
            return new StringContent($"\"{csvBase64}\"", Encoding.UTF8, "application/json");
        }

        private static StringContent GetStringContent(InventoryItem item)
        {
            var csvData = $"{item.Barcode},{item.StorageLocationName},{item.ItemDescription},{item.CreatedDate}";
            var csvBase64 = Convert.ToBase64String(Encoding.UTF8.GetBytes(csvData));
            return new StringContent($"\"{csvBase64}\"", Encoding.UTF8, "application/json");
        }

        private void SetUpClient()
        {
            Client = _factory.WithWebHostBuilder(builder =>
                builder.ConfigureServices(services =>
                {
                    // Remove existing DbContext registration
                    var descriptor = services.SingleOrDefault(
                        d => d.ServiceType == typeof(DbContextOptions<InventoryDbContext>));

                    if (descriptor != null)
                    {
                        services.Remove(descriptor);
                    }

                    // Add a new DbContext for testing with in-memory SQLite database
                    services.AddDbContext<InventoryDbContext>(options =>
                    {
                        //options.UseSqlite("Data Source=:memory:")
                        //       .EnableSensitiveDataLogging();
                        options.UseInMemoryDatabase("InventoryDb");
                    });

                    // Build a service provider to instantiate the context
                    var sp = services.BuildServiceProvider();

                    // Create a new scope to setup the database
                    using var scope = sp.CreateScope();                    
                    var scopedServices = scope.ServiceProvider;
                    _db = scopedServices.GetRequiredService<InventoryDbContext>();
                    _db.Database.OpenConnection();
                    _db.Database.EnsureCreated();

                    _db.SaveChanges();

                    // Clear local context cache
                    foreach (var entity in _db.ChangeTracker.Entries().ToList())
                    {
                        entity.State = EntityState.Detached;
                    }

                })).CreateClient();
        }

        private void ResetDatabase()
        {
            if (_db != null)
            {
                _db.Database.EnsureDeleted();
                _db.Database.EnsureCreated();
            }
        }
    }
}