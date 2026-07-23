namespace Business.Models.Interfaces
{
    public interface IEntityWithDiscordRole
    {
        string Color { get; set; }
        string Name { get; set; }
        ulong? DiscordRoleId { get; set; }

        void UpdateRole();
        void CheckRoleOnUser(ulong unitId);
    }
}
