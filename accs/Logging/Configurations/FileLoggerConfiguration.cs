namespace accs.Logging.Configurations
{
    public sealed class FileLoggerConfiguration
    {
		public int MaxFiles { get; set; } = 20;
		public string DirectoryPath { get; set; } = "logs";
		public LogLevel LoggingLevel { get; set; } = LogLevel.Trace;
	}
}
