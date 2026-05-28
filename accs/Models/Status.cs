using accs.Models.Enums;
using accs.Models.Interfaces;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace accs.Models
{
    public class Status : IEntityWithDiscordRole
    {
		[Key]
		[DatabaseGenerated(DatabaseGeneratedOption.None)]
		public StatusType Type { get; set; }
		public virtual List<UnitStatus> UnitStatuses { get; set; } = new List<UnitStatus>();
		public string Color { get; set; } = "#FFFFFF";
		public string Name { get; set; } = string.Empty;
		public ulong? DiscordRoleId { get; set; }

		public Status(string? envRoleString = null)
		{
			DiscordRoleId = envRoleString != null ? ulong.Parse(DotNetEnv.Env.GetString(envRoleString, $"{envRoleString} Not found")) : null;
		}
		public Status()
		{
			
		}

        public override string ToString()
        {
            return Type.ToString();
        }
	}
}
