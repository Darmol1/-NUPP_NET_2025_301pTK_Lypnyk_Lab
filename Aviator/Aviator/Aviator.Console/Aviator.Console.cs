using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Aviator.Common;
using Aviator.Common;

namespace Aviator.ConsoleApp
{
    class Program
    {
        static async Task Main(string[] args)
        {
            Console.WriteLine("=== Aviation demo: Async CRUD + Parallel generation ===");

        // файл для збереження згенерованої колекції
        var filePath = "buses_collection.json";

            // створюємо асинхронний CRUD-сервіс для Bus
            var busService = new CrudServiceAsync<Bus>(filePath);

            // Приклад використання примітивів синхронізації
            Console.WriteLine("\n-- Sync primitives examples --");
            SyncPrimitivesExamples.LockExample();
            await SyncPrimitivesExamples.SemaphoreExampleAsync();
            SyncPrimitivesExamples.AutoResetEventExample();
            SyncPrimitivesExamples.ManualResetEventSlimExample();

            Console.WriteLine("\n-- Parallel creation of 1000 Bus objects --");

            int totalToCreate = 1000;
            // Використаємо Parallel.For + синхронне блокування на CreateAsync (демонстрація Parallel)
            Parallel.For(0, totalToCreate, i =>
            {
                var bus = Bus.CreateNew();
                // Викликаємо асинхронний CreateAsync у синхронному контексті (GetAwaiter().GetResult()) —
                // це небажано в production, але демонструє використання Parallel.
                var created = busService.CreateAsync(bus).GetAwaiter().GetResult();
                if (!created)
                {
                    Console.WriteLine($"Не вдалося додати bus {bus.Id}");
                }
            });

            // Після створення — збережемо у файл
            var saved = await busService.SaveAsync();
            Console.WriteLine($"Збереження у файл '{filePath}' завершено: {saved}");

            // Читаємо усі елементи (snapshot)
            var all = (await busService.ReadAllAsync()).ToList();
            Console.WriteLine($"Кількість створених автобусів: {all.Count}");

            // Знайдемо мінімальні, максимальні та середні значення для числових полів
            if (all.Any())
            {
                var capacities = all.Select(b => b.Capacity).ToList();
                var mileages = all.Select(b => b.Mileage).ToList();
                var prices = all.Select(b => b.Price).ToList();

                Console.WriteLine("\n-- Статистика для згенерованих автобусів --");
                Console.WriteLine($"Capacity: min={capacities.Min()}, max={capacities.Max()}, avg={capacities.Average():F2}");
                Console.WriteLine($"Mileage (thousands km): min={mileages.Min():F2}, max={mileages.Max():F2}, avg={mileages.Average():F2}");
                Console.WriteLine($"Price (USD): min={prices.Min():F2}, max={prices.Max():F2}, avg={prices.Average():F2}");
            }

            // Демонструємо пагінацію: виведемо перші 2 сторінки по 5 елементів
            Console.WriteLine("\n-- Pagination sample (page, amount) --");
            var page1 = (await busService.ReadAllAsync(1, 5)).ToList();
            var page2 = (await busService.ReadAllAsync(2, 5)).ToList();

            Console.WriteLine($"Page 1 (5 items):");
            foreach (var b in page1) Console.WriteLine(b);

            Console.WriteLine($"\nPage 2 (5 items):");
            foreach (var b in page2) Console.WriteLine(b);

            Console.WriteLine("\n=== Demo finished ===");
        }
    }

}
