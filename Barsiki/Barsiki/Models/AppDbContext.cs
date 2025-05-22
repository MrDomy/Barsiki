using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;

namespace Barsiki.Models
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
        {
        }

        public DbSet<WateringRecord> WateringRecords { get; set; }
    }
}
