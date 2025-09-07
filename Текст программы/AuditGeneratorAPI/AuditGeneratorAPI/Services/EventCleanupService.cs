using AuditGeneratorAPI.Models;
using Microsoft.EntityFrameworkCore;

namespace AuditGeneratorAPI.Services
{
    public class EventCleanupService : BackgroundService
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly TimeSpan _interval = TimeSpan.FromHours(12); // Запускаем раз в 12 часов

        public EventCleanupService(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                await CleanupExpiredEventsAsync();

                // Ждем следующий запуск
                await Task.Delay(_interval, stoppingToken);
            }
        }

        private async Task CleanupExpiredEventsAsync()
        {
            using (var scope = _serviceProvider.CreateScope())
            {
                var context = scope.ServiceProvider.GetRequiredService<AuditGeneratorDbContext>();

                var now = DateTime.Now.Date; // Берем только текущую дату без времени

                var expiredEvents = await context.Events
                    .Where(e => e.Date < now)
                    .ToListAsync();

                if (expiredEvents.Any())
                {
                    context.Events.RemoveRange(expiredEvents);
                    await context.SaveChangesAsync();
                    Console.WriteLine($"Удалено устаревших мероприятий: {expiredEvents.Count}");
                }
                else
                {
                    Console.WriteLine("Устаренных мероприятий нет.");
                }
            }
        }
    }
}
