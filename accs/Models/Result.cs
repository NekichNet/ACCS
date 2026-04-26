using Sprache;

namespace accs.Models
{
    public class Result<T>
    {
        public T? Value { get; private set; }
        public Exception? Exception { get; private set; }
        public string Message { get; private set; } = "not stated";
        public bool IsSuccess { get; private set; } = false;
        public DateTime Started { get; } = DateTime.Now;
        public DateTime End { get; private set; }

        public Result<T> FormSuccess(T value, string message, Exception? exception = null)
        {
            End = DateTime.Now;
            IsSuccess = true;

            Value = value;
            Message = message;
            Exception = exception;

            return this;
        }

		public Result<T> FormException(Exception exception)
        {
            End = DateTime.Now;
            IsSuccess = false;

            Message = exception.Message;
            Exception = exception;

            return this;
        }

		public Result<T> FormFailure(string message, Exception? exception = null)
        {
            End = DateTime.Now;
            IsSuccess = false;

            Message = message;
            Exception = exception;

            return this;
        }
    }
}
