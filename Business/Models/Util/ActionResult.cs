using System.Text.Json;

namespace Business.Models.Util
{
    public class ActionResult<T> : EmptyAction
    {
        public T? Value { get; set; }

        public ActionResult(ILogger logger)
        {
            _logger = logger;
        }

		public new ActionResult<T> FormSuccess(string message, Exception? exception = null, int eventId = 200)
		{
			End = DateTime.UtcNow;
			IsSuccess = true;
			Message = message;
			Exception = exception;

			Log(eventId: eventId);

			return this;
		}

		public new ActionResult<T> FormException(Exception exception, int eventId = 400)
		{
			End = DateTime.UtcNow;
			IsSuccess = false;
			Message = exception.Message;
			Exception = exception;

			Log(LogLevel.Error, eventId);

			return this;
		}

		public new ActionResult<T> FormFailure(string message, Exception? exception = null, int eventId = 400)
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
