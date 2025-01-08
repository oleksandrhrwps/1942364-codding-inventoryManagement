using InventoryManagement.WebAPI;
using InventoryManagement.WebAPI.Data;
using InventoryManagement.WebAPI.Models;
using InventoryManagement.WebAPI.Models.Dto;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
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

        private const string CsvFilePath = "ItemsCollection.csv";
        private const string LogFilePath = "http_log.txt";

        public HttpClient Client { get; private set; }
        private InventoryDbContext _db;

        public IntegrationTests(WebApplicationFactory<Program> factory)
        {
            _factory = factory;
            SetUpClient();
        }

        #region middleware tests

        [Fact]
        public async Task Test_LoggingMiddleware_LogExists()
        {
            RemoveLogFile();
            
            if (!await SendVerifyItemRequests())
            {
                Assert.Fail("Failed to send verify item requests");
            }

            var logExists = File.Exists(LogFilePath);
            Assert.True(logExists);

        }

        [Fact]
        public async Task Test_LoggingMiddleware_RequestLogged()
        {
            RemoveLogFile();

            if (!await SendVerifyItemRequests())
            {
                Assert.Fail("Failed to send verify item requests");
            }

            var logExists = File.Exists(LogFilePath);

            Assert.True(logExists);

            var logContent = await File.ReadAllTextAsync(LogFilePath);
            Assert.Contains($"Request: POST {ApiEndpointVerifyItem}", logContent);
            Assert.Contains($"Request: POST {ApiEndpointUploadCsv}", logContent);
        }

        [Fact]
        public async Task Test_LoggingMiddleware_HeadersLogged()
        {
            RemoveLogFile();

            if (!await SendVerifyItemRequests())
            {
                Assert.Fail("Failed to send verify item requests");
            }

            var logExists = File.Exists(LogFilePath);

            Assert.True(logExists);

            var logContent = await File.ReadAllTextAsync(LogFilePath);
            Assert.Contains("Headers:", logContent);
            Assert.Contains("[Content-Type, application/json; charset=utf-8]", logContent);
            Assert.Contains("[Content-Length, 118]", logContent);
            Assert.Contains("[Host, localhost]", logContent);
            Assert.Contains("[Content-Type, text/plain; charset=utf-8]", logContent);
        }

        [Fact]
        public async Task Test_LoggingMiddleware_ResponseLogged()
        {
            RemoveLogFile();

            if (!await SendVerifyItemRequests())
            {
                Assert.Fail("Failed to send verify item requests");
            }

            var logExists = File.Exists(LogFilePath);

            Assert.True(logExists);

            var logContent = await File.ReadAllTextAsync(LogFilePath);
            Assert.Contains($"Response: 200", logContent);
        }

        [Fact]
        public async Task Test_LoggingMiddleware_TimeLogged()
        {
            RemoveLogFile();

            if (!await SendVerifyItemRequests())
            {
                Assert.Fail("Failed to send verify item requests");
            }

            var logExists = File.Exists(LogFilePath);

            Assert.True(logExists);

            var logContent = await File.ReadAllTextAsync(LogFilePath);
            Assert.Contains($"Time taken:", logContent);
        }

        #endregion

        #region Endpoints Tests

        [Fact]
        public async Task Test_UploadCsv_Returns200()
        {
            var stringContent = await GetStringContentAsync(CsvFilePath);
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
                CreatedDate = DateTime.Now.AddDays(30),
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
            var verifyResponce = new ItemVerificationDto
            {
                Barcode = Guid.NewGuid(),
                StorageLocationName = "SampleLocation",
            };

            var response = await Client.PostAsync(ApiEndpointVerifyItem, GetStringContent(verifyResponce));

            Assert.Equal(System.Net.HttpStatusCode.NotFound, response.StatusCode);
        }

        [Fact]
        public async Task Test_VerifyItem_Returns200()
        {
            ResetDatabase();

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

            var verifyResponce = new ItemVerificationDto
            {
                Barcode = generatedGuid,
                StorageLocationName = locationName,
            };

            var response = await Client.PostAsync(ApiEndpointVerifyItem, GetStringContent(verifyResponce));

            Assert.Equal(System.Net.HttpStatusCode.OK, response.StatusCode);
        }

        [Fact]
        public async Task Test_VerifyItem_Returns400()
        {
            ResetDatabase();

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

            var verifyResponce = new ItemVerificationDto
            {
                Barcode = generatedGuid,
                StorageLocationName = expectedLocationName,
            };

            var response = await Client.PostAsync(ApiEndpointVerifyItem, GetStringContent(verifyResponce));

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

            _db.DiscrepancyRecords.AddRange(testData);

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

        #endregion

        #region Helper Methods

        private static async Task<StringContent> GetStringContentAsync(string csvFilePath)
        {
            var csvData = await File.ReadAllTextAsync(csvFilePath);
            var csvBase64 = Convert.ToBase64String(Encoding.UTF8.GetBytes(csvData));
            return new StringContent($"\"{csvBase64}\"", Encoding.UTF8, "application/json");
        }

        private static StringContent GetStringContent(InventoryItem item)
        {
            var stringData = $"{item.Barcode},{item.StorageLocationName},{item.ItemDescription},{item.CreatedDate}";
            var stringBase64 = Convert.ToBase64String(Encoding.UTF8.GetBytes(stringData));
            return new StringContent($"\"{stringBase64}\"", Encoding.UTF8, "application/json");
        }

        private static StringContent GetStringContent(object item)
        {
            var stringData = JsonSerializer.Serialize(item);
            return new StringContent(stringData, Encoding.UTF8, "application/json");
        }

        private async Task<bool> SendVerifyItemRequests()
        {
            ResetDatabase();

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

            if (responsePrepareData.StatusCode != System.Net.HttpStatusCode.OK)
            {
                return false;
            }

            var verifyResponce = new ItemVerificationDto
            {
                Barcode = generatedGuid,
                StorageLocationName = locationName,
            };

            var response = await Client.PostAsync(ApiEndpointVerifyItem, GetStringContent(verifyResponce));

            if (response.StatusCode != System.Net.HttpStatusCode.OK)
            {
                return false;
            }

            return true;
        }

        private void SetUpClient()
        {
            Client = _factory.WithWebHostBuilder(builder =>
                builder.UseStartup<Startup>()
                .ConfigureServices(services =>
                {
                    _db = new InventoryDbContext(new DbContextOptionsBuilder<InventoryDbContext>()
                        .UseSqlite("DataSource=:memory:")
                        .EnableSensitiveDataLogging()
                        .Options);

                    services.RemoveAll(typeof(InventoryDbContext));
                    services.AddSingleton(_db);

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

        private void RemoveLogFile()
        {
            if (File.Exists(LogFilePath))
            {
                File.Delete(LogFilePath);
            }
        }

        #endregion

        public void Dispose()
        {
            _db?.Dispose();
            Client?.Dispose();
        }
    }
}