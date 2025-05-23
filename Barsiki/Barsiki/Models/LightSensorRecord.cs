using System;
using System.ComponentModel.DataAnnotations;

namespace Barsiki.Models
{
    public class LightSensorRecord
    {
        public int Id { get; set; }
        public DateTime Timestamp { get; set; } = DateTime.Now;
        public int LightLevel { get; set; }
        public string Status { get; set; } = "normal";
    }
}