using accs.Logging.Configurations;
using accs.Logging.Providers;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging.Configuration;

namespace accs.Logging.Extensions
{
    public static class FileLoggerExtensions
    {
        public static ILoggingBuilder AddFile(
			this ILoggingBuilder builder)
        {
			builder.AddConfiguration();

			builder.Services.TryAddEnumerable(
				ServiceDescriptor.Singleton<ILoggerProvider, FileLoggerProvider>());

			LoggerProviderOptions.RegisterProviderOptions
				<FileLoggerConfiguration, FileLoggerProvider>(builder.Services);

			return builder;
		}

		public static ILoggingBuilder AddFile(
			this ILoggingBuilder builder,
			Action<FileLoggerConfiguration> configure)
		{
			builder.AddFile();
			builder.Services.Configure(configure);

			return builder;
		}
	}
}
