using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace Aviator.Common
{
    // ====== Інтерфейс CRUD (синхронний) ======
    public interface ICrudService<T>
    {
        void Create(T element);
        T Read(Guid id);
        IEnumerable<T> ReadAll();
        void Update(T element);
        void Remove(T element);
    }

// ====== Асинхронний інтерфейс CRUD + IEnumerable ======
public interface ICrudServiceAsync<T> : IEnumerable<T>
    {
        Task<bool> CreateAsync(T element);
        Task<T> ReadAsync(Guid id);
        Task<IEnumerable<T>> ReadAllAsync();
        Task<IEnumerable<T>> ReadAllAsync(int page, int amount);
        Task<bool> UpdateAsync(T element);
        Task<bool> RemoveAsync(T element);
        Task<bool> SaveAsync();
    }

    // ====== Базовий клас ======
    public abstract class Entity
    {
        public Guid Id { get; set; }
        public DateTime CreatedAt { get; set; }

        public Entity()
        {
            Id = Guid.NewGuid();
            CreatedAt = DateTime.Now;
        }
    }

    // ====== Клас Aircraft ======
    public class Aircraft : Entity
    {
        public string Model { get; set; }
        public int Capacity { get; set; }
        public double Range { get; set; }

        public static int TotalAircraft;

        static Aircraft()
        {
            TotalAircraft = 0;
        }

        public Aircraft(string model, int capacity, double range)
        {
            Model = model;
            Capacity = capacity;
            Range = range;
            TotalAircraft++;
        }

        public override string ToString()
        {
            return $"{Model}, {Capacity} місць, {Range} км дальність";
        }
    }

    // ====== Клас Pilot ======
    public class Pilot : Entity
    {
        public string Name { get; set; }
        public int ExperienceYears { get; set; }
        public string LicenseNumber { get; set; }

        public Pilot(string name, int experience, string license)
        {
            Name = name;
            ExperienceYears = experience;
            LicenseNumber = license;
        }

        public void Fly(Aircraft aircraft)
        {
            Console.WriteLine($"Пілот {Name} керує літаком {aircraft.Model}");
        }
    }

    // ====== Клас Flight ======
    public class Flight : Entity
    {
        public string Code { get; set; }
        public Aircraft Aircraft { get; set; }
        public Pilot Pilot { get; set; }
        public DateTime DepartureTime { get; set; }

        public Flight(string code, Aircraft aircraft, Pilot pilot, DateTime departure)
        {
            Code = code;
            Aircraft = aircraft;
            Pilot = pilot;
            DepartureTime = departure;
        }

        public override string ToString()
        {
            return $"Рейс {Code}: {Aircraft.Model}, пілот {Pilot.Name}, виліт {DepartureTime}";
        }
    }

    // ====== Dispatcher (успадковує Pilot) ======
    public class Dispatcher : Pilot
    {
        public string Rank { get; set; }

        public Dispatcher(string name, int exp, string license, string rank)
            : base(name, exp, license)
        {
            Rank = rank;
        }

        public void ApproveFlight(Flight flight)
        {
            Console.WriteLine($"Диспетчер {Name} ({Rank}) підтвердив рейс {flight.Code}");
        }
    }

    // ====== Сервіс із подією ======
    public delegate void FlightAddedHandler(Flight flight);

    public class AviationService
    {
        public event FlightAddedHandler OnFlightAdded;

        public void AddFlight(Flight flight)
        {
            Console.WriteLine($"Рейс додано: {flight.Code}");
            OnFlightAdded?.Invoke(flight);
        }
    }

    // ====== Метод-розширення ======
    public static class PilotExtensions
    {
        public static bool IsVeteran(this Pilot pilot)
        {
            return pilot.ExperienceYears > 10;
        }
    }

    // ====== Клас Bus з CreateNew() ======
    public class Bus : Entity
    {
        private static readonly Random _rnd = new Random();

        public string Model { get; set; }
        public int Capacity { get; set; }         // кількість місць
        public double Mileage { get; set; }      // пробіг (тисячі км)
        public decimal Price { get; set; }       // ціна

        public Bus() { }

        public override string ToString()
        {
            return $"{Model} — місць: {Capacity}, пробіг: {Mileage}k km, ціна: {Price} USD";
        }

        // Статичний метод, який створює об'єкт із випадковими (генерованими) даними
        public static Bus CreateNew()
        {
            // lock random to be thread-safe for Random usage
            lock (_rnd)
            {
                var capacityOptions = new[] { 20, 25, 30, 35, 40, 45, 50 };
                var model = $"Bus-{_rnd.Next(1000, 9999)}";
                var capacity = capacityOptions[_rnd.Next(capacityOptions.Length)];
                var mileage = Math.Round((_rnd.NextDouble() * 400.0) + 10.0, 2); // 10.00 .. 410.00 (thousands km)
                var price = Math.Round((decimal)(_rnd.NextDouble() * 150000 + 20000), 2); // 20k .. 170k
                return new Bus
                {
                    Model = model,
                    Capacity = capacity,
                    Mileage = mileage,
                    Price = price
                };
            }
        }
    }

    // ====== Асинхронний, багатопотоково-безпечний CRUD-сервіс ======
    // Зберігає у ConcurrentDictionary для швидкого доступу і веде чергу GUID для порядку вставки.
    public class CrudServiceAsync<T> : ICrudServiceAsync<T> where T : Entity
    {
        private readonly ConcurrentDictionary<Guid, T> _storage = new ConcurrentDictionary<Guid, T>();
        private readonly ConcurrentQueue<Guid> _order = new ConcurrentQueue<Guid>();
        private readonly string _filePath;
        private readonly SemaphoreSlim _fileSemaphore = new SemaphoreSlim(1, 1); // для серіалізації/десеріалізації файлу
        private readonly object _enumerationLock = new object(); // приклад lock для синхронізації GetEnumerator

        // public FilePath property (заданий при створенні)
        public string FilePath => _filePath;

        public CrudServiceAsync(string filePath = "data.json")
        {
            _filePath = filePath ?? "data.json";
            // Якщо файл існує — спробуємо завантажити при створенні (блокуємо, але це демонстраційно OK)
            if (File.Exists(_filePath))
            {
                // Завантажимо синхронно результат асинхронної операції для простоти і гарантії початкового стану.
                // У production краще використовувати фабрику CreateFromFileAsync.
                try
                {
                    var json = File.ReadAllText(_filePath);
                    var list = JsonSerializer.Deserialize<List<T>>(json);
                    if (list != null)
                    {
                        foreach (var item in list)
                        {
                            _storage[item.Id] = item;
                            _order.Enqueue(item.Id);
                        }
                    }
                }
                catch
                {
                    // якщо файл пошкоджено — ігноруємо (можна логувати)
                }
            }
        }

        // Альтернативна фабрика для асинхронного створення з файлу
        public static async Task<CrudServiceAsync<T>> CreateFromFileAsync(string filePath)
        {
            var svc = new CrudServiceAsync<T>(filePath);
            // якщо файл існує та потрібно асинхронно прочитати — зробимо це
            if (File.Exists(filePath))
            {
                await svc._fileSemaphore.WaitAsync().ConfigureAwait(false);
                try
                {
                    var json = await File.ReadAllTextAsync(filePath).ConfigureAwait(false);
                    var list = JsonSerializer.Deserialize<List<T>>(json);
                    if (list != null)
                    {
                        foreach (var item in list)
                        {
                            svc._storage[item.Id] = item;
                            svc._order.Enqueue(item.Id);
                        }
                    }
                }
                catch
                {
                }
                finally
                {
                    svc._fileSemaphore.Release();
                }
            }
            return svc;
        }

        public async Task<bool> CreateAsync(T element)
        {
            if (element == null) throw new ArgumentNullException(nameof(element));
            // додаємо в словник та чергу
            var added = _storage.TryAdd(element.Id, element);
            if (added)
            {
                _order.Enqueue(element.Id);
            }
            // для демонстрації — не зберігаємо файл автоматично, SaveAsync викликається явно
            await Task.CompletedTask;
            return added;
        }

        public async Task<T> ReadAsync(Guid id)
        {
            _storage.TryGetValue(id, out var value);
            await Task.CompletedTask;
            return value;
        }

        public async Task<IEnumerable<T>> ReadAllAsync()
        {
            // Повертаємо snapshot в порядку інсерту
            var list = GetSnapshotOrdered();
            return await Task.FromResult(list);
        }

        public async Task<IEnumerable<T>> ReadAllAsync(int page, int amount)
        {
            if (page < 1) page = 1;
            if (amount < 1) amount = 10;
            var ordered = GetSnapshotOrdered();
            var paged = ordered.Skip((page - 1) * amount).Take(amount).ToList();
            return await Task.FromResult(paged);
        }

        public async Task<bool> UpdateAsync(T element)
        {
            if (element == null) throw new ArgumentNullException(nameof(element));
            if (!_storage.ContainsKey(element.Id)) return await Task.FromResult(false);
            _storage[element.Id] = element; // ConcurrentDictionary indexer замінює значення atomically
            return await Task.FromResult(true);
        }

        public async Task<bool> RemoveAsync(T element)
        {
            if (element == null) throw new ArgumentNullException(nameof(element));
            var removed = _storage.TryRemove(element.Id, out var _);
            // Проста логіка — залишаємо id в черзі (щоб зберігати порядок вставок).
            // Якщо потрібно видаляти з порядку, це складніше для ConcurrentQueue.
            return await Task.FromResult(removed);
        }

        public async Task<bool> SaveAsync()
        {
            // серіалізуємо snapshot в файл асинхронно у потокобезпечний спосіб
            var list = GetSnapshotOrdered().ToList();
            var options = new JsonSerializerOptions { WriteIndented = true };
            var json = JsonSerializer.Serialize(list, options);

            await _fileSemaphore.WaitAsync().ConfigureAwait(false);
            try
            {
                await File.WriteAllTextAsync(_filePath, json).ConfigureAwait(false);
                return true;
            }
            catch
            {
                return false;
            }
            finally
            {
                _fileSemaphore.Release();
            }
        }

        // Допоміжні методи
        private IEnumerable<T> GetSnapshotOrdered()
        {
            // Отримуємо snapshot значень у порядку вставки,
            // пропускаючи ті, що були видалені.
            var result = new List<T>();
            foreach (var id in _order)
            {
                if (_storage.TryGetValue(id, out var item))
                {
                    result.Add(item);
                }
            }
            return result;
        }

        // ====== IEnumerable<T> ======
        public IEnumerator<T> GetEnumerator()
        {
            // Блокуємо невеликий проміжок для стабільності нтерування.
            lock (_enumerationLock)
            {
                // Повертаємо snapshot, щоб ітератор не залежав від конкурентних змін
                var snapshot = GetSnapshotOrdered().ToList();
                return snapshot.GetEnumerator();
            }
        }

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    }

    // ====== Клас для демонстрації примітивів синхронізації ======
    public static class SyncPrimitivesExamples
    {
        // Приклад використання lock (Monitor) для критичної секції
        public static void LockExample()
        {
            object _lock = new object();
            int counter = 0;
            void Work()
            {
                lock (_lock)
                {
                    counter++;
                }
            }
            Parallel.For(0, 1000, i => Work());
            Console.WriteLine($"LockExample counter = {counter}");
        }

        // Приклад SemaphoreSlim для обмеження одночасних потоків
        public static async Task SemaphoreExampleAsync()
        {
            var semaphore = new SemaphoreSlim(3); // максимум 3 одночасно
            int running = 0;
            var tasks = Enumerable.Range(0, 10).Select(async i =>
            {
                await semaphore.WaitAsync();
                try
                {
                    Interlocked.Increment(ref running);
                    Console.WriteLine($"Task {i} started, running={running}");
                    await Task.Delay(100);
                }
                finally
                {
                    Interlocked.Decrement(ref running);
                    semaphore.Release();
                }
            }).ToArray();

            await Task.WhenAll(tasks);
            Console.WriteLine("SemaphoreExampleAsync finished");
        }

        // Приклад AutoResetEvent для простого сигналу між потоками
        public static void AutoResetEventExample()
        {
            var are = new AutoResetEvent(false);
            int value = 0;

            var t = new Thread(() =>
            {
                // дочекаємось сигналу
                are.WaitOne();
                value = 42;
                Console.WriteLine("Worker set value to 42");
            });
            t.Start();

            // Основний потік дає сигнал
            Thread.Sleep(50);
            are.Set();

            t.Join();
            Console.WriteLine($"AutoResetEventExample value = {value}");
        }

        // Інші приклади (ManualResetEventSlim)
        public static void ManualResetEventSlimExample()
        {
            var mres = new ManualResetEventSlim(false);
            int value = 0;

            var t = new Thread(() =>
            {
                mres.Wait();
                value = 100;
            });
            t.Start();

            // дозволяємо іншим потокам продовжувати
            mres.Set();
            t.Join();
            Console.WriteLine($"ManualResetEventSlimExample value = {value}");
        }
    }

}
