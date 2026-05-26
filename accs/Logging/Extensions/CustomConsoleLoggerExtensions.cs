using accs.Logging.Configurations;
using accs.Logging.Providers;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging.Configuration;

namespace accs.Logging.Extensions
{
    public static class CustomConsoleLoggerExtensions
    {
		public static ILoggingBuilder AddCustomConsole(
			this ILoggingBuilder builder)
		{
			builder.AddConfiguration();

			builder.Services.TryAddEnumerable(
				ServiceDescriptor.Singleton<ILoggerProvider, CustomConsoleLoggerProvider>());

			LoggerProviderOptions.RegisterProviderOptions
				<CustomConsoleLoggerConfiguration, CustomConsoleLoggerProvider>(builder.Services);

			return builder;
		}

		public static ILoggingBuilder AddCustomConsole(
			this ILoggingBuilder builder,
			Action<CustomConsoleLoggerConfiguration> configure)
		{
			builder.AddCustomConsole();
			builder.Services.Configure(configure);

			return builder;
		}
	}
}
