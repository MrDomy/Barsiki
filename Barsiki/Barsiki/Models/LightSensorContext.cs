using Microsoft.EntityFrameworkCore;

namespace Barsiki.Models
{
    public class LightSensorContext : DbContext
    {
        public LightSensorContext(DbContextOptions<LightSensorContext> options)
            : base(options) { }

        public DbSet<LightSensorRecord> LightSensorRecords { get; set; }
    }
}