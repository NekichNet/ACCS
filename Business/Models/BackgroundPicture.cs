using Business.Models.Abstraction;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Business.Models
{
	public class BackgroundPicture : IEntityWithFiles
    {
        public int Id { get; set; }
        public string Name { get; set; }
        [JsonIgnore] public virtual List<Unit> Units { get; set; }

        public string GetFilesFolderName()
        {
            return "backgrounds";
        }

        public override string ToString()
        {
            return JsonSerializer.Serialize(this);
        }
    }

	public class BackgroundPictureConfiguration : IEntityTypeConfiguration<BackgroundPicture>
	{
		public void Configure(EntityTypeBuilder<BackgroundPicture> builder)
		{
			builder.HasMany(fk => fk.Units).WithOne(u => u.BackgroundPicture).OnDelete(DeleteBehavior.SetNull);
		}
	}
}
