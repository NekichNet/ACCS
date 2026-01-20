namespace accs.Services.Interfaces
{
    public interface IEasyOCRService
    {
        public Task<List<string>> ReceiveNamesFromPhoto(string imagePath);
    }
}
