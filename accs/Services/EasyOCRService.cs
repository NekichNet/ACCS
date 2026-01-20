using accs.Models;
using accs.Repository;
using accs.Repository.Interfaces;
using accs.Services.Interfaces;
using Discord.WebSocket;
using IronOcr;


namespace accs.Services
{
    public class EasyOCRService : IEasyOCRService
    {
        private IUnitRepository _unitRepository;
        private DiscordSocketClient _discordSocketClient;
        private SocketGuild? _guild { get { return _discordSocketClient.GetGuild(0); } } // Подставь айдишник сервера
        public EasyOCRService(IUnitRepository unitRepository, DiscordSocketClient discordSocketClient) 
        {
            _unitRepository = unitRepository;
            _discordSocketClient = discordSocketClient;
        }
        public async Task<List<string>> ReceiveNamesFromPhoto(string imagePath)
        {
            Dictionary<Unit, int> MostMatches = new Dictionary<Unit, int>(); 
            var text = new IronTesseract().Read(imagePath).Lines;
            var units = await _unitRepository.ReadAllAsync();

            foreach (var unit in units) //перевернуть вверх-дном
            {
                MostMatches.Add(unit, 0);
                for (int k = 0; k < unit.Nickname.Length-3; k++)
                {
                    var symbolsToCompare = unit.Nickname.Substring(k, 3);
                    foreach (var line in text)
                    {
                        if (line.Text.Contains(symbolsToCompare))
                        {
                            MostMatches[unit]++;
                        }
                    }
                }
            }

            return new List<string>(text.ToList());
        }
    }
    
}
