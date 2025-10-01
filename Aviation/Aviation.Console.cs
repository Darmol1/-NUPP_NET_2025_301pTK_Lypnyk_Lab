using System;
using Aviation.Common;

namespace Aviation.ConsoleApp
{
    class Program
    {
        static void Main(string[] args)
        {
            var aircraft = new Aircraft("Boeing 737", 180, 5000);
            var pilot = new Pilot("Олександр Коваль", 12, "LIC123");
            var dispatcher = new Dispatcher("Марія Сидоренко", 20, "LIC999", "Старший диспетчер");

            var flight = new Flight("PS101", aircraft, pilot, DateTime.Now.AddHours(2));

            // CRUD сервіс
            var flightService = new CrudService<Flight>();
            flightService.Create(flight);

            foreach (var f in flightService.ReadAll())
            {
                System.Console.WriteLine(f);
            }

            // події
            var avService = new AviationService();
            avService.OnFlightAdded += (f) => System.Console.WriteLine($"Подія: новий рейс {f.Code}");
            avService.AddFlight(flight);

            // метод-розширення
            System.Console.WriteLine($"Чи пілот {pilot.Name} ветеран? {pilot.IsVeteran()}");

            // наслідуваний клас
            dispatcher.ApproveFlight(flight);
        }
    }
}
