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
        public async Task<IActionResult> CurrentState()
        {
            var lastRecord = await _context.WateringRecords
                .OrderByDescending(r => r.Time)
                .FirstOrDefaultAsync();

            if (lastRecord == null)
                return Json(new { Moisture = 0, Watering = false });

            return Json(new
            {
                Moisture = lastRecord.MoistureLevel,
                Watering = lastRecord.WateringEnabled,
                Time = lastRecord.Time.ToString("HH:mm:ss"),
                Source = lastRecord.EventType
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
        public async Task<IActionResult> SetMode(string mode) // "manual" или "automatic"
        {
            var lastRecord = await _context.WateringRecords
                .OrderByDescending(r => r.Time)
                .FirstOrDefaultAsync();

            var record = new WateringRecord
            {
                Time = DateTime.Now,
                EventType = mode,
                MoistureLevel = lastRecord?.MoistureLevel ?? 0,
                WateringEnabled = lastRecord?.WateringEnabled ?? false
            };

            _context.WateringRecords.Add(record);
            await _context.SaveChangesAsync();

            return RedirectToAction("Index");
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

        [HttpGet]

        // В WateringController.cs
        public async Task<IActionResult> GetHistoryPartial()
        {
            var records = await _context.WateringRecords
                .OrderByDescending(r => r.Time)
                .Take(20)
                .ToListAsync();

            return PartialView("_WateringHistoryPartial", records);
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