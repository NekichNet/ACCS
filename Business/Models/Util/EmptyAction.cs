using Business.Logging;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Business.Models.Util
{
    public class EmptyAction
    {
		protected ILogger? _logger;

		public string Message { get; set; } = "Not stated";
		public bool IsSuccess { get; set; }
        [JsonIgnore]
        public Exception? Exception { get; set; }
		public DateTime Start { get; set; }
		public DateTime End { get; set; }

		public EmptyAction(ILogger? logger = null)
		{
			_logger = logger;
			Start = DateTime.UtcNow;
		}

		public void Log(LogLevel logLevel = LogLevel.Trace, int eventId = 100)
		{
			if (_logger != null)
				_logger.Log(logLevel, ToString());
		}

		public EmptyAction FormSuccess(string message, Exception? exception = null, int eventId = 200)
		{
			End = DateTime.UtcNow;
			IsSuccess = true;
			Message = message;
			Exception = exception;

			Log(eventId: eventId);

			return this;
		}

		public EmptyAction FormException(Exception exception, int eventId = 400)
		{
			End = DateTime.UtcNow;
			IsSuccess = false;
			Message = exception.Message;
			Exception = exception;

			Log(LogLevel.Error, eventId);

			return this;
		}

		public EmptyAction FormFailure(string message, Exception? exception = null, int eventId = 400)
		{
			End = DateTime.UtcNow;
			IsSuccess = false;
			Message = message;
			Exception = exception;

			Log(LogLevel.Debug, eventId);

			return this;
		}

        public override string ToString()
        {
            return JsonSerializer.Serialize(this);
        }
	}
}
