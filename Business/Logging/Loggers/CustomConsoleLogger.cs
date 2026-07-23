using Business.Logging.Configurations;

namespace Business.Logging.Loggers
{
    public sealed class CustomConsoleLogger(
        string name,
		Func<CustomConsoleLoggerConfiguration> getCurrentConfig
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
				CustomConsoleLoggerConfiguration config = getCurrentConfig();

				lock (Lock)
				{
					ConsoleColor originalForegroundColor = Console.ForegroundColor;

					Console.ForegroundColor = ConsoleColor.Gray;

					Console.Write($"[" +
								$"{now.ToShortDateString()}, " +
								$"{now.Hour}:{now.Minute}:{now.Second}, ");

					Console.ForegroundColor = config.ColorMap[logLevel];

					Console.Write($"{logLevel}");

					Console.ForegroundColor = ConsoleColor.Gray;

					Console.Write($", {eventId.Name}, " +
								$"{name}" +
								$"]\n");

					Console.ForegroundColor = originalForegroundColor;

					Console.WriteLine(formatter(state, exception));
				}
			}
		}
	}
}
