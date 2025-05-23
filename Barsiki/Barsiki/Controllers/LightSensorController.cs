using Barsiki.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Linq;
using System.Threading.Tasks;

namespace Barsiki.Controllers
{
    public class LightSensorController : Controller
    {
        private readonly LightSensorContext _context;

        public LightSensorController(LightSensorContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> Index()
        {
            var latest = await _context.LightSensorRecords
                .OrderByDescending(r => r.Timestamp)
                .FirstOrDefaultAsync();

            var history = await _context.LightSensorRecords
                .OrderByDescending(r => r.Timestamp)
                .Take(20)
                .ToListAsync();

            return View(new LightSensorViewModel
            {
                CurrentLight = latest?.LightLevel ?? 0,
                CurrentStatus = latest?.Status ?? "normal",
                History = history
            });
        }

        [HttpGet]
        public async Task<IActionResult> GetCurrentLight()
        {
            var record = await _context.LightSensorRecords
                .OrderByDescending(r => r.Timestamp)
                .FirstOrDefaultAsync();

            return Json(new
            {
                LightLevel = record?.LightLevel ?? 0,
                Status = record?.Status ?? "normal",
                Time = record?.Timestamp.ToString("HH:mm:ss") ?? ""
            });
        }
    }

    public class LightSensorViewModel
    {
        public int CurrentLight { get; set; }
        public string CurrentStatus { get; set; }
        public List<LightSensorRecord> History { get; set; }
    }
}