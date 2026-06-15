namespace accs.Logging.Configurations
{
    public sealed class CustomConsoleLoggerConfiguration
    {
		public LogLevel LoggingLevel { get; set; } = LogLevel.Trace;
		public Dictionary<LogLevel, ConsoleColor> ColorMap { get; set; } = new()
		{
			[LogLevel.Trace] = ConsoleColor.Gray,
			[LogLevel.Debug] = ConsoleColor.Magenta,
			[LogLevel.Information] = ConsoleColor.Green,
			[LogLevel.Warning] = ConsoleColor.Yellow,
			[LogLevel.Error] = ConsoleColor.Red,
			[LogLevel.Critical] = ConsoleColor.DarkRed,
			[LogLevel.None] = ConsoleColor.White,
		};
	}
}
