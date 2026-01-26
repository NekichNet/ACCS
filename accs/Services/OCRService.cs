using accs.Database;
using accs.Models;
using accs.Models.Enums;
using accs.Services.Interfaces;
using Discord.WebSocket;
using EasyOcrSharp.Models;
using EasyOcrSharp.Services;
using Python.Runtime;
using Sprache;


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
            var loggerFactory = LoggerFactory.Create(builder =>
            {
                builder.AddSimpleConsole();
                builder.SetMinimumLevel(LogLevel.Information);
            });
            await using var ocr = new EasyOcrService(logger: loggerFactory.CreateLogger<EasyOcrService>());
            var result = await ocr.ExtractTextFromImage("sample.png", new[] { "en", "ru" });

            List<Unit> units = await _unitRepository.ReadAllAsync();

            HashSet<Unit> exitMatches = new HashSet<Unit>();

            foreach (var line in result.Lines)
            {
                Dictionary<Unit, int> matches = new Dictionary<Unit, int>();
                foreach (Unit unit in units)
                {
                    matches.Add(unit, 0);
                    for (int i = 0; i < line.Text.Length - ChunkSize; i++)
                    {
                        if (unit.Nickname.Contains(line.Text.Substring(i, ChunkSize)))
                        {
                            matches[unit]++;
                        }
                    }
				}
                Unit mostMatched = matches.MaxBy(m => m.Value).Key;
                exitMatches.Add(mostMatched);
                await _logService.WriteAsync($"line = {line.Text}; matched = {exitMatches.Last().Nickname}", LoggingLevel.Debug);
            }
            /*
            OcrResult.Line[] lines = new IronTesseract().Read(imagePath).Lines;

            */

            return exitMatches;
        }
    }
}
