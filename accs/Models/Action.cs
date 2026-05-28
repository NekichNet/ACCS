using System.Text.Json.Serialization;
using accs.Models.Interfaces;

namespace accs.Models
{
    public class Action<T> : IAction
    {
		public Unit? Actor { get; set; }
        public T? Value { get; set; }
		public Exception? Exception { get; set; }
        public string Message { get; set; } = "not stated";
        public bool IsSuccess { get; set; } = false;
		public DateTime Start { get; set; } = DateTime.UtcNow;
		public DateTime End { get; set; }

        public Action<T> FormSuccess(T value, string message, Exception? exception = null)
        {
            End = DateTime.UtcNow;
            IsSuccess = true;

            Value = value;
            Message = message;
            Exception = exception;

            return this;
        }

		public Action<T> FormException(Exception exception)
        {
            End = DateTime.UtcNow;
            IsSuccess = false;

            Message = exception.Message;
            Exception = exception;

            return this;
        }

		public Action<T> FormFailure(string message, Exception? exception = null)
        {
            End = DateTime.UtcNow;
            IsSuccess = false;

            Message = message;
            Exception = exception;

            return this;
        }
    }
}
