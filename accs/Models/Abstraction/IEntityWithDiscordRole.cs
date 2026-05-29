namespace accs.Models.Interfaces
{
    public interface IEntityWithDiscordRole
    {
        abstract string Color { get; set; }
        abstract string Name { get; set; }
        abstract ulong? DiscordRoleId { get; set; }

        void UpdateRole();
        void CheckRoleOnUser(ulong unitId);
    }
}
