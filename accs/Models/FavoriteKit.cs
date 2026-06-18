using accs.Models.Abstraction;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace accs.Models
{
	public class FavoriteKit : IEntityWithFiles
    {
        public int Id { get; set; }
        public string Name { get; set; }
        [JsonIgnore] public virtual List<Unit> Units { get; set; }

        public string GetFolderName()
        {
            return "kits";
        }

        public override string ToString()
        {
            return JsonSerializer.Serialize(this);
        }
    }

	public class FavoriteKitConfiguration : IEntityTypeConfiguration<FavoriteKit>
	{
		public void Configure(EntityTypeBuilder<FavoriteKit> builder)
		{
            builder.HasMany(fk => fk.Units).WithOne(u => u.FavoriteKit).OnDelete(DeleteBehavior.SetNull);
		}
	}
}
