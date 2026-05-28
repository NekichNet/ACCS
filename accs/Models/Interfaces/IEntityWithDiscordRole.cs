namespace accs.Models.Interfaces
{
    public interface IEntityWithDiscordRole
    {
        public abstract string Color { get; set; }
        public abstract string Name { get; set; }
        public abstract ulong? DiscordRoleId { get; set; }

        void UpdateRole();
        void CheckRoleOnUser(ulong unitId);
    }
}
