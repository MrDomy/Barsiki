using Barsiki.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace Barsiki.Controllers
{
    public class WateringController : Controller
    {
        private readonly AppDbContext _context;
        private const int MoistureThreshold = 30;

        public WateringController(AppDbContext context)
        {
            _context = context;
        }
        [HttpGet]
        public async Task<IActionResult> GetCurrentStatus()
        {
            var latest = await _context.WateringRecords
                .OrderByDescending(r => r.Time)
                .FirstOrDefaultAsync();

            if (latest == null) return NotFound();

            return Json(new
            {
                currentMoisture = latest.MoistureLevel,
                isWatering = latest.WateringEnabled,
                eventType = latest.EventType
            });
        }

        public async Task<IActionResult> Index()
        {
            var latestRecord = await _context.WateringRecords
                .OrderByDescending(r => r.Time)
                .FirstOrDefaultAsync();

            var model = new WateringViewModel
            {
                CurrentMoisture = latestRecord?.MoistureLevel ?? 50,
                IsWatering = latestRecord?.WateringEnabled ?? false,
                MoistureThreshold = MoistureThreshold,
                EventType = latestRecord?.EventType ?? "sensor_read",
                History = await _context.WateringRecords
                    .OrderByDescending(r => r.Time)
                    .Take(20)
                    .ToListAsync()
            };

            return View(model);
        }

        [HttpPost]
        public async Task<IActionResult> ToggleWatering(bool enable)
        {
            var lastRecord = await _context.WateringRecords
                .OrderByDescending(r => r.Time)
                .FirstOrDefaultAsync();

            var record = new WateringRecord
            {
                Time = DateTime.Now,
                EventType = "manual",
                MoistureLevel = lastRecord?.MoistureLevel ?? 50,
                WateringEnabled = enable
            };

            _context.WateringRecords.Add(record);
            await _context.SaveChangesAsync();

            return RedirectToAction("Index");
        }
    }

    public class WateringViewModel
    {
        public int CurrentMoisture { get; set; }
        public bool IsWatering { get; set; }
        public int MoistureThreshold { get; set; }
        public string EventType { get; set; }
        public List<WateringRecord> History { get; set; }
    }
}