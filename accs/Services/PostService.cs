using accs.Models;
using accs.Services.Interfaces;

namespace accs.Services
{
    public class PostService : BusinessService
    {
        Models.Action<Post> Create(
            string name,
            string description,
            int? subdivisionId,
            int headId
            )
    }
}
