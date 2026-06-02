using accs.Models.Abstraction;

namespace accs.Models
{
    public class BackgroundPicture : IEntityWithImage
    {
        public int Id { get; set; }
        public string Name { get; set; }

        public string GetImageFolderName()
        {
            return "backgrounds";
        }
    }
}
