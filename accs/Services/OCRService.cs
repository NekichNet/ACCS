using accs.Database;
using accs.Models;
using accs.Models.Enum;
using accs.Services.Interfaces;
using Discord.WebSocket;
using IronOcr;


namespace accs.Services
{
    public class OCRService : IOCRService
    {
        private readonly AppDbContext _db;
        private ILogService _logService;

		public static int ChunkSize { get; private set; } = 3;

        public OCRService(AppDbContext db, DiscordSocketClient discordSocketClient, ILogService logService) 
        {
            _db = db;
            _logService = logService;
		}

        public async Task<HashSet<Unit>> ReceiveNamesFromPhoto(string imagePath)
        {
            OcrResult.Line[] lines = new IronTesseract().Read(imagePath).Lines;
            List<Unit> units = _db.Units.ToList();
            HashSet<Unit> result = new HashSet<Unit>();

            foreach (OcrResult.Line line in lines)
            {
				Dictionary<Unit, int> matches = new Dictionary<Unit, int>();
                foreach (Unit unit in units)
                {
                    matches.Add(unit, 0);
                    for (int i = 0; i < line.Text.Length - ChunkSize; i++)
                        if (unit.Nickname.Contains(line.Text.Substring(i, ChunkSize)))
                            matches[unit]++;
				}
                Unit mostMatched = matches.MaxBy(m => m.Value).Key;
				result.Add(mostMatched);
                await _logService.WriteAsync($"line = {line.Text}; matched = {result.Last().Nickname}", LoggingLevel.Debug);
			}

            return result;
        }
    }
}
