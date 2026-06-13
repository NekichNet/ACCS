using accs.Models.SingleDayEvents.Abstraction;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace accs.Models
{
	public class Doc
	{
		public int Id { get; set; }
		public string Name { get; set; }
		public ulong? AuthorId { get; set; }
		[JsonIgnore] public virtual Unit? Author { get; set; }

        public override string ToString()
        {
            return JsonSerializer.Serialize(this);
        }
	}

    public class DocConfiguration : IEntityTypeConfiguration<Doc>
    {
        public void Configure(EntityTypeBuilder<Doc> builder)
        {
            builder.HasOne(d => d.Author).WithMany(u => u.OwnDocs).HasForeignKey(u => u.AuthorId).OnDelete(DeleteBehavior.NoAction);
        }
    }
}
