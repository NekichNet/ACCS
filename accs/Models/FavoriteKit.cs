using accs.Models.Abstraction;
using System.Text.Json;

namespace accs.Models
{
    public class FavoriteKit : IEntityWithFiles
    {
        public int Id { get; set; }
        public string Name { get; set; }

        public string GetImageFolderName()
        {
            return "kits";
        }

        public override string ToString()
        {
            return JsonSerializer.Serialize(this);
        }
    }
}
