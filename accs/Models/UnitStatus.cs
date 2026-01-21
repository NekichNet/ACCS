namespace accs.Models
{
    public class UnitStatus // Для любых временных статусов
    {
        public int Id { get; set; }
        public Unit Unit { get; set; }
        public Status Status { get; set; }
        public DateTime Start { get; set; } = DateTime.UtcNow;
        public DateTime? End { get; set; }
    }
}
