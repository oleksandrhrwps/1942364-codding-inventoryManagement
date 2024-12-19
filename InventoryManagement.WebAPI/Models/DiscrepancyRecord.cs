namespace InventoryManagement.WebAPI.Models
{
    public class DiscrepancyRecord
    {
        public Guid Barcode { get; set; }
        public DateTime ScanningDate { get; set; }
        public string ActualStorageLocation { get; set; }
    }
}
