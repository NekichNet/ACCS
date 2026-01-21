using accs.Models;
using accs.Services.Interfaces;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace accs.Services
{
    public class LogService : ILogService
    {
		public int MaxFilesCount { get; set; }
        public string LogDirectoryPath { get; set; } = string.Empty;
		public LoggingLevel FileLogLevel { get; set; }
		public LoggingLevel ConsoleLogLevel { get; set; }

		public LogService()
        {
			MaxFilesCount = DotNetEnv.Env.GetInt("MAX_FILES_COUNT", -1);
			LogDirectoryPath = DotNetEnv.Env.GetString("LOG_DIRECTORY_PATH", Path.Join("accs-logs"));
            FileLogLevel = (LoggingLevel)DotNetEnv.Env.GetInt("FILE_LOG_LEVEL", 3);
			FileLogLevel = (LoggingLevel)DotNetEnv.Env.GetInt("CONSOLE_LOG_LEVEL", 3);
		}

        public async Task WriteAsync(string text, LoggingLevel level = LoggingLevel.Info)
        {
            DateTime now = DateTime.Now;
			File.AppendAllText(Path.Join(LogDirectoryPath, now.ToShortDateString() + ".txt"), $"[{level.ToString()}][{now.Hour}:{now.Minute}:{now.Second}]{text}\n");
		}

        public async Task<Exception> ExceptionAsync(string text, LoggingLevel level = LoggingLevel.Fatal)
        {
            await WriteAsync(text, level);
            return new Exception(text);
        }
    }
}
