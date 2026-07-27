using Business.Models.Abstraction;
using Business.Models.SingleDayEvents.Abstraction;
using Business.Models.States.Abstraction;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Business.Models
{
	public class Doc : IEntityWithFiles
	{
		public int Id { get; set; }
		public string Title { get; set; }
        public DateTime UploadedTime { get; set; }
        public bool IsHidden { get; set; } = false;
		public ulong AuthorId { get; set; }
		[JsonIgnore] public virtual Unit Author { get; set; }
        public virtual List<EventWithDoc> Events { get; set; }
        public virtual List<StateWithDoc> States { get; set; }

		public string GetFilesFolderName()
        {
            return "docs";
        }

        public override string ToString()
        {
            return JsonSerializer.Serialize(this);
        }
	}

    /// <summary>
    /// Общепринятое DTO для загрузки нового документа
    /// </summary>
	public class NewDocDto
	{
		public string Title { get; set; } = "Новый документ";
		public HashSet<int> UnitIds { get; set; } = new HashSet<int>();
		public IFormFile File { get; set; }
	}

	public class DocConfiguration : IEntityTypeConfiguration<Doc>
    {
        public void Configure(EntityTypeBuilder<Doc> builder)
        {
            builder.HasOne(d => d.Author).WithMany(u => u.OwnDocs).HasForeignKey(u => u.AuthorId).OnDelete(DeleteBehavior.NoAction);
        }
    }
}
