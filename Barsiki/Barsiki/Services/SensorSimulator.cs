using Barsiki.Models;
using Microsoft.EntityFrameworkCore;

namespace Barsiki.Services
{
    public class SensorSimulator : IHostedService, IDisposable
    {
        private readonly IServiceProvider _services;
        private Timer _timer;
        private int _currentMoisture = 20;
        private bool _isWatering = false;
        private string _currentMode = "automatic"; // "manual" или "automatic"
        private DateTime _lastDbUpdate = DateTime.MinValue;
        private const int DryThreshold = 30;
        private const int WetThreshold = 70;

        public SensorSimulator(IServiceProvider services)
        {
            _services = services;
        }

        public Task StartAsync(CancellationToken cancellationToken)
        {
            _timer = new Timer(async _ => await ProcessSensorData(), null, TimeSpan.Zero, TimeSpan.FromSeconds(1));
            return Task.CompletedTask;
        }

        private async Task ProcessSensorData()
        {
            using var scope = _services.CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();

            try
            {
                // Получаем текущий режим из последней записи
                var lastRecord = await dbContext.WateringRecords
                    .OrderByDescending(r => r.Time)
                    .FirstOrDefaultAsync();

                if (lastRecord != null)
                {
                    _currentMode = lastRecord.EventType == "manual" ? "manual" : "automatic";
                    _isWatering = lastRecord.WateringEnabled;
                }

                // В автоматическом режиме включаем/выключаем по влажности
                if (_currentMode == "automatic")
                {
                    if (_currentMoisture < DryThreshold) _isWatering = true;
                    else if (_currentMoisture > WetThreshold) _isWatering = false;
                }

                // Обновляем влажность
                _currentMoisture += _isWatering ? 1 : -1;
                _currentMoisture = Math.Clamp(_currentMoisture, 0, 100);

                // Сохраняем в базу, если прошло >10 секунд или изменилось состояние полива
                bool shouldWrite = lastRecord == null ||
                    (DateTime.Now - _lastDbUpdate).TotalSeconds > 1 ||
                    _isWatering != lastRecord.WateringEnabled;

                if (shouldWrite)
                {
                    dbContext.WateringRecords.Add(new WateringRecord
                    {
                        Time = DateTime.Now,
                        EventType = _currentMode,
                        MoistureLevel = _currentMoisture,
                        WateringEnabled = _isWatering
                    });

                    await dbContext.SaveChangesAsync();
                    _lastDbUpdate = DateTime.Now;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка: {ex.Message}");
            }
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            _timer?.Change(Timeout.Infinite, 0);
            return Task.CompletedTask;
        }

        public void Dispose()
        {
            _timer?.Dispose();
        }
    }
}