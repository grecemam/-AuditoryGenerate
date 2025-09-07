using AuditGeneratorAPI.Models;
using Microsoft.EntityFrameworkCore;

namespace AuditGeneratorAPI.Services
{
    public class StudyPracticeCleanupService : BackgroundService
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly TimeSpan _interval = TimeSpan.FromHours(12); // Запускаем раз в 12 часов

        public StudyPracticeCleanupService(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                await CleanupExpiredPracticesAsync();

                // Ждем следующий запуск
                await Task.Delay(_interval, stoppingToken);
            }
        }

        private async Task CleanupExpiredPracticesAsync()
        {
            using (var scope = _serviceProvider.CreateScope())
            {
                var context = scope.ServiceProvider.GetRequiredService<AuditGeneratorDbContext>();

                var now = DateTime.Now.Date; // Берем только текущую дату без времени

                var expiredPractices = await context.StudyPractices
                    .Where(p => p.Date < now)
                    .ToListAsync();

                if (expiredPractices.Any())
                {
                    context.StudyPractices.RemoveRange(expiredPractices);
                    await context.SaveChangesAsync();
                    Console.WriteLine($"Удалено устаревших учебных практик: {expiredPractices.Count}");
                }
                else
                {
                    Console.WriteLine("Устаревших учебных практик нет.");
                }
            }
        }
    }
}
