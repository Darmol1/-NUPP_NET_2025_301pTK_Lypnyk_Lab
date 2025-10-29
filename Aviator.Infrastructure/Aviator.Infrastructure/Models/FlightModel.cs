namespace Aviator.Infrastructure.Models
{
    public class FlightModel
    {
        public int Id { get; set; }               // Наприклад: "F1"
        public string FlightNumber { get; set; }     // Номер рейсу (FL100)
        public DateTime Departure { get; set; }      // Час вильоту
        public DateTime Arrival { get; set; }        // Час прибуття

        // Зовнішні ключі
        public int AircraftModelId { get; set; }  // Посилання на літак
        public AircraftModel Aircraft { get; set; }

        public int PilotModelId { get; set; }     // Посилання на пілота
        public PilotModel Pilot { get; set; }
    }
}
