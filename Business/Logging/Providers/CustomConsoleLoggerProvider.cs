using Business.Logging.Configurations;
using Business.Logging.Loggers;
using Microsoft.Extensions.Options;
using System.Collections.Concurrent;
using System.Runtime.Versioning;

namespace Business.Logging.Providers
{
	[UnsupportedOSPlatform("browser")]
	[ProviderAlias("CustomConsole")]
	public sealed class CustomConsoleLoggerProvider : ILoggerProvider
    {
		private readonly IDisposable? _onChangeToken;
		private CustomConsoleLoggerConfiguration _currentConfig;
		private readonly ConcurrentDictionary<string, CustomConsoleLogger> _loggers =
			new(StringComparer.OrdinalIgnoreCase);

		public CustomConsoleLoggerProvider(
		IOptionsMonitor<CustomConsoleLoggerConfiguration> config)
		{
			_currentConfig = config.CurrentValue;
			_onChangeToken = config.OnChange(updatedConfig => _currentConfig = updatedConfig);
		}

		public ILogger CreateLogger(string categoryName) =>
			_loggers.GetOrAdd(categoryName, name => new CustomConsoleLogger(name, GetCurrentConfig));

		private CustomConsoleLoggerConfiguration GetCurrentConfig() => _currentConfig;

		public void Dispose()
		{
			_loggers.Clear();
			_onChangeToken?.Dispose();
		}
	}
}
