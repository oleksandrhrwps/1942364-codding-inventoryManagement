namespace InventoryManagement.WebAPI.Models.Dto
{
    public class ItemVerificationDto
    {
        public Guid Barcode { get; set; }
        public string StorageLocationName { get; set; }
    }
}
