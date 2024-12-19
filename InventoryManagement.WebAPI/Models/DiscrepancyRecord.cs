namespace InventoryManagement.WebAPI.Models
{
    public class DiscrepancyRecord
    {
        public long Id { get; set; }
        public Guid Barcode { get; set; }
        public DateTime ScanningDate { get; set; }
        public string ActualStorageLocation { get; set; }
    }
}
