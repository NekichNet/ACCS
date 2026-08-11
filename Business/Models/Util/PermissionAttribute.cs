namespace Business.Models.Util
{
	[AttributeUsage(AttributeTargets.Field)]
	public class PermissionAttribute : Attribute
	{
		public string Name { get; set; } = string.Empty;
		public string Description { get; set; } = string.Empty;
	}
}
