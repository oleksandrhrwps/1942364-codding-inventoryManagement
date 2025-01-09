using InventoryManagement.WebAPI.Models.Dto;
using Microsoft.AspNetCore.Mvc;

namespace InventoryManagement.WebAPI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class InventoryController : ControllerBase
    {
        public InventoryController()
        {
            
        }

        [HttpPost("upload-csv")]
        public async Task<IActionResult> UploadCsv([FromBody] string csvData)
        {
            throw new NotImplementedException();
        }

        [HttpPost("verify-item")]
        public async Task<IActionResult> VerifyItem([FromBody] ItemVerificationDto itemVerification)
        {
            throw new NotImplementedException();
        }

        [HttpGet("discrepancies")]
        public async Task<IActionResult> GetDiscrepancies(DateTime? scanningDate = null, string? storageLocation = null)
        {
            throw new NotImplementedException();
        }

    }
}
