namespace accs.Models.Database
{
    public class AssignedReward
    {
        public int RewardId { get; set; }
        public virtual Reward Reward { get; set; }
        public int UnitId { get; set; }
        public virtual Unit Unit { get; set; }
        public DateOnly AssignedDate { get; set; }
        public bool Display { get; set; }

        public override string ToString()
        {
            return RewardId.ToString() + " " + UnitId.ToString();
        }
    }
}
