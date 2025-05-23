using Barsiki.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Barsiki.Services
{
    public class LightSensorService : IHostedService, IDisposable
    {
        private readonly IServiceProvider _services;
        private readonly ILogger<LightSensorService> _logger;
        private Timer _timer;
        private readonly Random _random = new();
        private int _currentLightLevel = 500;
        private const int UpdateIntervalSeconds = 5;

        public LightSensorService(IServiceProvider services, ILogger<LightSensorService> logger)
        {
            _services = services;
            _logger = logger;
        }

        public Task StartAsync(CancellationToken cancellationToken)
        {
            _logger.LogInformation("Starting Light Sensor Service");
            _timer = new Timer(
                callback: ExecuteLightMeasurement,
                state: null,
                dueTime: TimeSpan.Zero,
                period: TimeSpan.FromSeconds(UpdateIntervalSeconds));

            return Task.CompletedTask;
        }

        private async void ExecuteLightMeasurement(object state)
        {
            try
            {
                using var scope = _services.CreateScope();
                using var dbContext = scope.ServiceProvider.GetRequiredService<LightSensorContext>();

                // Генерация данных
                _currentLightLevel = GenerateLightLevel();
                var status = DetermineLightStatus(_currentLightLevel);

                // Создание записи
                var record = new LightSensorRecord
                {
                    Timestamp = DateTime.Now,
                    LightLevel = _currentLightLevel,
                    Status = status
                };

                // Правильное обращение к DbSet
                await dbContext.LightSensorRecords.AddAsync(record);
                await dbContext.SaveChangesAsync();

                _logger.LogDebug($"Recorded light level: {_currentLightLevel} lux, Status: {status}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in light sensor measurement");
            }
        }

        private int GenerateLightLevel()
        {
            // Реалистичная симуляция освещенности
            double timeFactor = Math.Sin((DateTime.Now.TimeOfDay.TotalHours - 6) / 12 * Math.PI);
            int baseLevel = 500 + (int)(300 * timeFactor);
            int variation = _random.Next(-50, 51);
            return Math.Clamp(baseLevel + variation, 0, 1000);
        }

        private string DetermineLightStatus(int lightLevel)
        {
            return lightLevel switch
            {
                < 100 => "low",
                > 900 => "high",
                _ => "normal"
            };
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            _logger.LogInformation("Stopping Light Sensor Service");
            _timer?.Change(Timeout.Infinite, 0);
            return Task.CompletedTask;
        }

        public void Dispose()
        {
            _timer?.Dispose();
            _logger.LogInformation("Light Sensor Service disposed");
        }
    }
}