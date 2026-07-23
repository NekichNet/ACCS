using Business.Logging.Configurations;

namespace Business.Logging.Loggers
{
	public sealed class FileLogger(
        string name,
        Func<FileLoggerConfiguration> getCurrentConfig
        ) : ILogger, IDisposable
    {
        public static object Lock = new object();

		public IDisposable BeginScope<TState>(TState state)
			where TState : notnull => default!;

		public void Dispose() { }

        public bool IsEnabled(LogLevel logLevel) =>
            getCurrentConfig().LoggingLevel <= logLevel;

        public void Log<TState>(
            LogLevel logLevel,
            EventId eventId,
            TState state,
            Exception? exception,
            Func<TState, Exception?, string> formatter)
        {
            if (IsEnabled(logLevel))
            {
                DateTime now = DateTime.Now;
				string filepath = Path.Join(getCurrentConfig().DirectoryPath, $"{now.Year}-{now.Month}-{now.Day}.log");

				lock (Lock)
				{
					DeleteExcessFiles();
					
                    using (StreamWriter writer = new StreamWriter(filepath, true, System.Text.Encoding.UTF8))
                    {
                        writer.WriteLine($"\n[" +
                            $"{now.ToShortDateString()}, " +
                            $"{now.Hour}:{now.Minute}:{now.Second}, " +
                            $"{logLevel, -12}, " +
                            $"{eventId.Name}, " +
                            $"{name}" +
                            $"]");
                        writer.WriteLine(formatter(state, exception));
                    }
				}
			}
		}

        public int CountFiles()
        {
            return Directory.EnumerateFiles(getCurrentConfig().DirectoryPath).Count();
        }

        public IEnumerable<string> GetFilePaths()
        {
            return Directory.EnumerateFiles(getCurrentConfig().DirectoryPath);
        }

        public void DeleteExcessFiles()
        {
            if (CreateDirIfNotExists())
            {
				List<string> filePaths = GetFilePaths().OrderBy(p => File.GetCreationTimeUtc(p)).ToList();
				while (filePaths.Count > getCurrentConfig().MaxFiles)
				{
					File.Delete(filePaths.First());
				}
			}
		}

        public bool CreateDirIfNotExists()
        {
            bool exists = Directory.Exists(getCurrentConfig().DirectoryPath);
			if (!exists)
				Directory.CreateDirectory(getCurrentConfig().DirectoryPath);
            return exists;
        }
    }
}
