using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using ReactiveUI;
using System.Reactive;

namespace Code.Views
{
	public partial class InputDialog : Window
	{
		public InputDialog()
		{
			InitializeComponent();
			var viewModel = new InputDialogViewModel(this);
			DataContext = viewModel;
		}

		private void InitializeComponent()
		{
			AvaloniaXamlLoader.Load(this);
		}
	}

	public class InputDialogViewModel : ReactiveObject
	{
		private string _message;
		private string _inputText;
		private readonly Window _window;

		public InputDialogViewModel(Window window)
		{
			_window = window;
			OkCommand = ReactiveCommand.Create(OnOk);
			CancelCommand = ReactiveCommand.Create(OnCancel);
		}

		public string Message
		{
			get => _message;
			set => this.RaiseAndSetIfChanged(ref _message, value);
		}

		public string InputText
		{
			get => _inputText;
			set => this.RaiseAndSetIfChanged(ref _inputText, value);
		}

		public ReactiveCommand<Unit, Unit> OkCommand { get; }
		public ReactiveCommand<Unit, Unit> CancelCommand { get; }

		private void OnOk()
		{
			_window.Close(InputText);
		}

		private void OnCancel()
		{
			_window.Close(null);
		}
	}
}
