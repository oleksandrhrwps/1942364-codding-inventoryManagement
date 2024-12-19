using InventoryManagement.WebAPI.Models.Dto;
using Microsoft.AspNetCore.Mvc;

namespace InventoryManagement.WebAPI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class InventoryController : ControllerBase
    {
        [HttpPost("upload-csv")]
        public IActionResult UploadCsv([FromBody] string csvData)
        {
            throw new NotImplementedException();
        }

        [HttpPost("verify-item")]
        public IActionResult VerifyItem([FromBody] ItemVerificationDto item)
        {
            throw new NotImplementedException();
        }

        [HttpGet("discrepancies")]
        public IActionResult GetDiscrepancies(DateTime? scanningDate = null, string storageLocation = "")
        {
            throw new NotImplementedException();
        }
    }
}
