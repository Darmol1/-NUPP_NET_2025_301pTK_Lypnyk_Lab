namespace Aviator.Infrastructure.Models
{
    public class AircraftModel
    {
        public int Id { get; set; }          // Наприклад: "A1"
        public string Name { get; set; }        // Назва літака
        public int Capacity { get; set; }       // Кількість місць

        // Зв’язок один-до-багатьох: один літак має багато рейсів
        public ICollection<FlightModel> Flights { get; set; } = new List<FlightModel>();
    }
}
