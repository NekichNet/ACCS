namespace accs.Models
{
    public class UnitStatus // Для любых временных статусов
    {
        public int Id { get; set; }
		public Unit Unit { get; set; }
		public Status Status { get; set; }
        public DateTime StartDate { get; set; } = DateTime.Today;
        public DateTime? EndDate { get; set; }
    }
}
