using Barsiki.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Barsiki.Services
{
    public class SensorSimulator : IHostedService, IDisposable
    {
        private readonly IServiceProvider _services;
        private Timer _timer;
        private int _currentMoisture = 50;
        private bool _isWatering = false;
        private DateTime _lastDbUpdate = DateTime.MinValue;
        private const int DryThreshold = 30;
        private const int WetThreshold = 70;
        private const int ManualOverrideDuration = 30; // секунд

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
                // Проверка на ручное управление
                var lastManual = await dbContext.WateringRecords
                    .Where(r => r.EventType == "manual")
                    .OrderByDescending(r => r.Time)
                    .FirstOrDefaultAsync();

                bool isManualMode = lastManual != null &&
                                    (DateTime.Now - lastManual.Time).TotalSeconds < ManualOverrideDuration;

                // Автоматическое управление поливом
                if (!isManualMode)
                {
                    if (_currentMoisture < DryThreshold && !_isWatering)
                        _isWatering = true;
                    else if (_currentMoisture > WetThreshold && _isWatering)
                        _isWatering = false;
                }

                // Обновляем влажность
                _currentMoisture += _isWatering ? 1 : -1;
                _currentMoisture = Math.Clamp(_currentMoisture, 0, 100);

                // Определение типа события
                string eventType = isManualMode ? "manual" :
                    (_isWatering && _currentMoisture < DryThreshold) ? "automatic" : "sensor_read";

                // Последняя запись
                var lastRecord = await dbContext.WateringRecords
                    .OrderByDescending(r => r.Time)
                    .FirstOrDefaultAsync();

                bool shouldWriteToDb = lastRecord == null ||
                    (DateTime.Now - _lastDbUpdate).TotalSeconds >= 10 ||
                    _isWatering != lastRecord.WateringEnabled;

                if (shouldWriteToDb)
                {
                    var record = new WateringRecord
                    {
                        Time = DateTime.Now,
                        EventType = eventType,
                        MoistureLevel = _currentMoisture,
                        WateringEnabled = _isWatering
                    };

                    dbContext.WateringRecords.Add(record);
                    await dbContext.SaveChangesAsync();
                    _lastDbUpdate = DateTime.Now;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка обработки данных: {ex.Message}");
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
