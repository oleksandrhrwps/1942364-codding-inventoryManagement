﻿namespace InventoryManagement.WebAPI.Models
{
    public class InventoryItem
    {
        public long Id { get; set; }
        public Guid Barcode { get; set; }
        public string StorageLocationName { get; set; }
        public string ItemDescription { get; set; }
        public DateTime CreatedDate { get; set; }
    }
}
