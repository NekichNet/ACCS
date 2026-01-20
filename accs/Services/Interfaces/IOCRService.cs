using accs.Models;

namespace accs.Services.Interfaces
{
    public interface IOCRService
    {
        public Task<HashSet<Unit>> ReceiveNamesFromPhoto(string imagePath);
    }
}
