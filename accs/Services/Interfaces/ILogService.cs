using accs.Models;

namespace accs.Services.Interfaces
{
    public interface ILogService
    {
        Task WriteAsync(string text, LoggingLevel level = LoggingLevel.Info);
        Task<Exception> ExceptionAsync(string text, LoggingLevel level = LoggingLevel.Fatal);
	}
}
