using System;
using System.ComponentModel.DataAnnotations;

namespace Barsiki.Models
{
    public class WateringRecord
    {
        public int Id { get; set; }

        [Display(Name = "Время")]
        public DateTime Time { get; set; }

        [Display(Name = "Тип события")]
        public string? EventType { get; set; } // "automatic", "manual", "sensor_read" (допускает null)

        [Display(Name = "Влажность")]
        public int MoistureLevel { get; set; }

        [Display(Name = "Полив включен")]
        public bool WateringEnabled { get; set; }
    }
}