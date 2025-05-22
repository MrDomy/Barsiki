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
        private DateTime _lastManualTime = DateTime.MinValue;
        private const int DryThreshold = 30;
        private const int WetThreshold = 70;
        private const int ManualOverrideDuration = 30; // секунды

        public SensorSimulator(IServiceProvider services)
        {
            _services = services;
        }

        public Task StartAsync(CancellationToken cancellationToken)
        {
            _timer = new Timer(SimulateSensor, null, TimeSpan.Zero, TimeSpan.FromSeconds(1));
            return Task.CompletedTask;
        }

        private async void SimulateSensor(object state)
        {
            using var scope = _services.CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();

            // Проверяем последнее ручное управление
            var lastManual = await dbContext.WateringRecords
                .Where(r => r.EventType == "manual")
                .OrderByDescending(r => r.Time)
                .FirstOrDefaultAsync();

            bool manualOverride = lastManual != null &&
                               (DateTime.Now - lastManual.Time).TotalSeconds < ManualOverrideDuration;

            // Автоматическое управление (если нет ручного переопределения)
            if (!manualOverride)
            {
                if (_currentMoisture < DryThreshold && !_isWatering)
                {
                    _isWatering = true;
                }
                else if (_currentMoisture > WetThreshold && _isWatering)
                {
                    _isWatering = false;
                }
            }

            // Изменение влажности
            if (_isWatering)
            {
                _currentMoisture = Math.Min(_currentMoisture + 2, 100);
            }
            else
            {
                _currentMoisture = Math.Max(_currentMoisture - 1, 0);
            }

            // Определяем тип события
            string eventType = manualOverride ? "manual" :
                             _isWatering && _currentMoisture < DryThreshold ? "automatic" :
                             "sensor_read";

            // Записываем в БД каждые 10 секунд или при изменении состояния
            var lastRecord = await dbContext.WateringRecords
                .OrderByDescending(r => r.Time)
                .FirstOrDefaultAsync();

            if (lastRecord == null ||
                (DateTime.Now - lastRecord.Time).TotalSeconds >= 10 ||
                _isWatering != lastRecord.WateringEnabled)
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