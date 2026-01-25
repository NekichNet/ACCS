using accs.Models.Enums;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace accs.Models
{
    public class Status
    {
		[Key]
		[DatabaseGenerated(DatabaseGeneratedOption.None)]
		public StatusType Type { get; set; }
        public string Name { get; set; } = string.Empty;
        public ulong? DiscordRoleId { get; set; }
		public virtual List<UnitStatus> UnitStatuses { get; set; } = new List<UnitStatus>();

		public Status(string? envRoleString = null)
		{
			DiscordRoleId = envRoleString != null ? ulong.Parse(DotNetEnv.Env.GetString(envRoleString, $"{envRoleString} Not found")) : null;
		}
		public Status()
		{
			
		}
	}
}
