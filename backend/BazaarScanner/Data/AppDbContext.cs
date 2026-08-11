using BazaarScanner.Models;
using Microsoft.EntityFrameworkCore;

namespace BazaarScanner.Data
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
        {
        }

        public DbSet<ScannedItem> Items { get; set; }
    }
}