using accs.Models.Configurations;
using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace accs.Models
{
	public class Permission
	{
		[Key]
		[DatabaseGenerated(DatabaseGeneratedOption.None)]
		public PermissionType Type { get; set; }
		public string Description { get; set; } = string.Empty;
		public List<Rank> Ranks { get; set; } = new List<Rank>();
		public List<Post> Posts { get; set; } = new List<Post>();
		public List<Subdivision> Subdivisions { get; set; } = new List<Subdivision>();
	}
}
