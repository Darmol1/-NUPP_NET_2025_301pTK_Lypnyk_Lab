namespace Aviator.Infrastructure.Models
{
    public class PilotModel
    {
        public int Id { get; set; }              // Наприклад: "P1"
        public string FullName { get; set; }        // Ім'я пілота
        public string LicenseNumber { get; set; }   // Номер ліцензії

        // Зв’язок один-до-багатьох: один пілот може виконувати багато рейсів
        public ICollection<FlightModel> Flights { get; set; } = new List<FlightModel>();
    }
}
