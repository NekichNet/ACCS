using System.Text.Json;

namespace accs.Models
{
    public class EmptyAction
    {
		protected ILogger? _logger;

		public string Message { get; set; } = "Not stated";
		public bool IsSuccess { get; set; }
		public Exception? Exception { get; set; }
		public DateTime Start { get; set; }
		public DateTime End { get; set; }

		public EmptyAction(ILogger? logger = null)
		{
			_logger = logger;
			Start = DateTime.UtcNow;
		}

		public void Log(LogLevel logLevel = LogLevel.Trace)
		{
			if (_logger != null)
				_logger.Log(logLevel, this.ToString());
		}

		public EmptyAction FormSuccess(string message, Exception? exception = null)
		{
			End = DateTime.UtcNow;
			IsSuccess = true;
			Message = message;
			Exception = exception;

			Log();

			return this;
		}

		public EmptyAction FormException(Exception exception)
		{
			End = DateTime.UtcNow;
			IsSuccess = false;
			Message = exception.Message;
			Exception = exception;

			Log(LogLevel.Error);

			return this;
		}

		public EmptyAction FormFailure(string message, Exception? exception = null)
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
