using Business.Database;
using Business.Logging;
using Business.Models;
using Business.Services.Interfaces;
using EasyOcrSharp.Services;
using Microsoft.EntityFrameworkCore;


namespace Business.Services
{
    public class OCRService
    {
        private readonly AppDbContext _db;
        private ILogger<OCRService> _log;
        private ILogger<EasyOcrService> _detailLog;

		public static int ChunkSize { get; private set; } = 3;

        public OCRService(AppDbContext db, ILogger<OCRService> log, ILogger<EasyOcrService> detailLog) 
        {
            _db = db;
            _log = log;
            _detailLog = detailLog;
		}

        public async Task<HashSet<Unit>> ReceiveNamesFromPhoto(string imagePath)
        {
            await using var ocr = new EasyOcrService(logger: _detailLog);
            var result = await ocr.ExtractTextFromImage(imagePath, new[] { "en", "ru" });

            List<Unit> units = await _db.Units.ToListAsync();

            HashSet<Unit> exitMatches = new HashSet<Unit>();

            foreach (var line in result.Lines)
            {
                Dictionary<Unit, int> matches = new Dictionary<Unit, int>();
                foreach (Unit unit in units)
                {
                    matches.Add(unit, 0);
                    for (int i = 0; i < line.Text.Length - ChunkSize; i++)
                        if (unit.Nickname.Contains(line.Text.Substring(i, ChunkSize)))
                            matches[unit]++;
				}
                KeyValuePair<Unit, int> mostMatched = matches.MaxBy(m => m.Value);
                if (mostMatched.Value / (mostMatched.Key.Nickname.Length - 2) >= 0.5)
                    exitMatches.Add(mostMatched.Key);
                _log.LogTrace(EventIds.Details, $"line = {line.Text}; matched = {exitMatches.Last().Nickname}");
            }

            return exitMatches;
        }
    }
}
