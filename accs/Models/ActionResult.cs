using Microsoft.AspNetCore.Mvc;
using System.Text.Json;

namespace accs.Models
{
    public class ActionResult<T> : EmptyAction
    {
        public T? Value { get; set; }

        public ActionResult(ILogger logger)
        {
            _logger = logger;
        }

		public new ActionResult<T> FormSuccess(string message, Exception? exception = null)
		{
			End = DateTime.UtcNow;
			IsSuccess = true;
			Message = message;
			Exception = exception;

			Log();

			return this;
		}

		public new ActionResult<T> FormException(Exception exception)
		{
			End = DateTime.UtcNow;
			IsSuccess = false;
			Message = exception.Message;
			Exception = exception;

			Log(LogLevel.Error);

			return this;
		}

		public new ActionResult<T> FormFailure(string message, Exception? exception = null)
		{
			End = DateTime.UtcNow;
			IsSuccess = false;
			Message = message;
			Exception = exception;

			Log(LogLevel.Debug);

			return this;
		}

		public override string ToString()
        {
            return JsonSerializer.Serialize(this);
        }
    }
}
