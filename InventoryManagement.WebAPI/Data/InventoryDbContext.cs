using InventoryManagement.WebAPI.Models;
using Microsoft.EntityFrameworkCore;

namespace InventoryManagement.WebAPI.Data
{
    public class InventoryDbContext : DbContext
    {
        public InventoryDbContext(DbContextOptions<InventoryDbContext> options) : base(options)
        {
        }

        public DbSet<InventoryItem> InventoryItems { get; set; }
        public DbSet<DiscrepancyRecord> Discrepancies { get; set; }
    }
}
