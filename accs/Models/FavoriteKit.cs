using accs.Models.Abstraction;

namespace accs.Models
{
    public class FavoriteKit : IEntityWithImage
    {
        public int Id { get; set; }
        public string Name { get; set; }

        public string GetImageFolderName()
        {
            return "kits";
        }
    }
}
