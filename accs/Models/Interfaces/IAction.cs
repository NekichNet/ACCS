using System.Text.Json.Serialization;

namespace accs.Models.Interfaces
{
    public interface IAction
    {
		[JsonIgnore] Unit? Actor { get; set; }
		string Message { get; set; }
		bool IsSuccess { get; set; }
		Exception? Exception { get; set; }
		[JsonIgnore] DateTime Start { get; set; }
		[JsonIgnore] DateTime End { get; set; }
	}
}
