using Aviator.Infrastructure;
using Aviator.Infrastructure.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Threading.Tasks;

class Program
{
    static async Task Main(string[] args)
    {
        var services = new ServiceCollection();

        var dbPath = Path.Combine(AppContext.BaseDirectory, "aviator.db");
        services.AddDbContext<AviatorContext>(options => options.UseSqlite($"Data Source={dbPath}"));

        services.AddScoped(typeof(IRepository<>), typeof(Repository<>));
        services.AddScoped(typeof(ICrudServiceAsync<>), typeof(CrudServiceAsync<>));

        var serviceProvider = services.BuildServiceProvider();

        var aircraftService = serviceProvider.GetRequiredService<ICrudServiceAsync<AircraftModel>>();

        var aircraft = new AircraftModel { Name = "Boeing 737", Capacity = 180 };
        await aircraftService.CreateAsync(aircraft);

        var allAircrafts = await aircraftService.ReadAllAsync();
        foreach (var a in allAircrafts)
        {
            Console.WriteLine($"{a.Id} - {a.Name} ({a.Capacity} seats)");
        }
    }
}
