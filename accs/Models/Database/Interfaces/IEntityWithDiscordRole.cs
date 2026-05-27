namespace accs.Models.Database.Interfaces
{
    public interface IEntityWithDiscordRole
    {
        public abstract string Color { get; set; }
        public abstract string Name { get; set; }
        public abstract ulong? DiscordRoleId { get; set; }

        public void UpdateRole()
        {
            if (DiscordRoleId != null)
            {
				// TODO: Add request to discord-bot project
			}
		}

		public void CheckRoleOnUser(ulong unitId)
        {
			if (DiscordRoleId != null)
			{
				// TODO: Add request to discord-bot project
			}
		}
    }
}
