namespace LivingAtlas.Desktop.ViewModels;

public sealed class StatusBarViewModel : ViewModelBase
{
	private string _message = "Ready";

	public string Message
	{
		get
		{
			return _message;
		}
		private set
		{
			SetProperty(ref _message, value, "Message");
		}
	}

	public void SetMessage(string message)
	{
		Message = message;
	}
}
