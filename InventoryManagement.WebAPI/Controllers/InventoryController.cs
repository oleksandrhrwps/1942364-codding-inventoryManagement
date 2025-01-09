using InventoryManagement.WebAPI.Data;
using InventoryManagement.WebAPI.Models;
using InventoryManagement.WebAPI.Models.Dto;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Text;

namespace InventoryManagement.WebAPI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class InventoryController : ControllerBase
    {
        private readonly InventoryDbContext _db;

        public InventoryController(InventoryDbContext db)
        {
            _db = db;
        }

        [HttpPost("upload-csv")]
        public async Task<IActionResult> UploadCsv([FromBody] string csvData)
        {
            byte[] data = Convert.FromBase64String(csvData);
            string csvContent = Encoding.UTF8.GetString(data);

            var (items, validationError) = ParseInventoryItemsFromCsv(csvContent);

            if (!string.IsNullOrWhiteSpace(validationError))
            {
                return BadRequest($"Invalid data: {validationError}");
            }

            var newItems = items.Where(item => !_db.InventoryItems.Any(existing => existing.Barcode == item.Barcode)).ToList();

            if (!newItems.Any())
            {
                return Ok("No new records to upload.");
            }

            _db.InventoryItems.AddRange(newItems);
            await _db.SaveChangesAsync();

            return Ok($"{newItems.Count} items uploaded successfully.");
        }

        [HttpPost("verify-item")]
        public async Task<IActionResult> VerifyItem([FromBody] ItemVerificationDto itemVerification)
        {
            var item = _db.InventoryItems.SingleOrDefault(i => i.Barcode == itemVerification.Barcode);
            if (item == null)
            {
                return NotFound();
            }

            if (!string.Equals(item.StorageLocationName, itemVerification.StorageLocationName, StringComparison.OrdinalIgnoreCase))
            {
                var discrepancyLog = new DiscrepancyRecord
                {
                    Barcode = itemVerification.Barcode,
                    ScanningDate = DateTime.UtcNow,
                    ActualStorageLocation = itemVerification.StorageLocationName,
                };

                _db.DiscrepancyRecords.Add(discrepancyLog);
                await _db.SaveChangesAsync();

                return BadRequest("Location discrepancy detected.");
            }

            return Ok();
        }

        [HttpGet("discrepancies")]
        public async Task<IActionResult> GetDiscrepancies(DateTime? scanningDate = null, string? storageLocation = null)
        {
            var query = _db.DiscrepancyRecords.AsQueryable();

            if (scanningDate.HasValue)
            {
                query = query.Where(d => d.ScanningDate.Date == scanningDate.Value.Date);
            }

            if (!string.IsNullOrWhiteSpace(storageLocation))
            {
                query = query.Where(d => d.ActualStorageLocation == storageLocation);
            }

            var result = await query.ToListAsync();

            return Ok(result);
        }

        private static (List<InventoryItem>, string) ParseInventoryItemsFromCsv(string csvContent)
        {
            var items = new List<InventoryItem>();
            var lines = csvContent.Split('\n');

            foreach (var line in lines)
            {
                if (string.IsNullOrWhiteSpace(line))
                {
                    continue;
                }

                var values = line.Split(',');

                if (values.Length < 4)
                {
                    return (items, "Each line must have at least 4 values.");
                }

                if (!Guid.TryParse(values[0], out var barcode))
                {
                    return (items, $"Invalid GUID found: {values[0]}");
                }

                var storageLocationName = values[1].Trim();

                if (string.IsNullOrWhiteSpace(storageLocationName))
                {
                    return (items, $"Storage Location Name cannot be empty or contains only spaces: {values[1]}");
                }

                var createdDate = DateTime.Parse(values[3].Trim());

                if (createdDate > DateTime.UtcNow)
                {
                    return (items, "Created Date cannot be in the future.");
                }

                var item = new InventoryItem
                {
                    Barcode = barcode,
                    StorageLocationName = storageLocationName,
                    ItemDescription = values.Length >= 3 ? values[2].Trim() : string.Empty,
                    CreatedDate = createdDate
                };

                items.Add(item);
            }

            return (items, string.Empty);
        }
    }
}
