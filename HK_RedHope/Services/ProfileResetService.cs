using HK_RedHope.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

public class ProfileResetService : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly TimeSpan _checkInterval = TimeSpan.FromHours(24); 

    public ProfileResetService(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            await CheckAndResetProfiles();
            await Task.Delay(_checkInterval, stoppingToken);
        }
    }

    private async Task CheckAndResetProfiles()
    {
        using (var scope = _serviceProvider.CreateScope())
        {
            var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

            var now = DateTime.Now;
            var usersToReset = await context.Users
                .Where(u => u.LastProfileUpdate != null &&
                            u.LastProfileUpdate.Value.AddDays(30) <= now)
                .ToListAsync();

            foreach (var user in usersToReset)
            {
                user.MedicalHistory = null;
                user.RiskBehavior = null;
                user.CurrentHealthStatus = null;
                user.IsPregnant = null;
                user.IsApproved = false;
                user.LastProfileUpdate = DateTime.Now;
            }

            if (usersToReset.Count > 0)
                await context.SaveChangesAsync();
        }
    }
}
