using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using Avalonia.Platform.Storage;
using Code.Views;
using ReactiveUI;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Reactive;
using System.Threading.Tasks;

namespace Code.Views
{
	public partial class MainWindow : Window
	{
		private Popup _suggestionsPopup;

		public MainWindow()
		{
			InitializeComponent();
			DataContext = new MainWindowViewModel(this);
			_suggestionsPopup = this.FindControl<Popup>("SuggestionsPopup");
		}

		private void InitializeComponent()
		{
			AvaloniaXamlLoader.Load(this);
		}

		private void OnKeyDown(object sender, KeyEventArgs e)
		{
			if (e.Key == Key.F2)
			{
				var viewModel = DataContext as MainWindowViewModel;
				viewModel?.RenameCommand.Execute().Subscribe();
			}
			else if (e.Key == Key.S && e.KeyModifiers == KeyModifiers.Control)
			{
				var viewModel = DataContext as MainWindowViewModel;
				viewModel?.SaveCommand.Execute().Subscribe();
			}
		}

		private void OnFileDoubleTapped(object sender, RoutedEventArgs e)
		{
			var viewModel = DataContext as MainWindowViewModel;
			viewModel?.OpenSelectedFile();
		}

		private void CodeTextBox_KeyDown(object sender, KeyEventArgs e)
		{
			var textBox = sender as TextBox;
			var viewModel = DataContext as MainWindowViewModel;

			if (textBox != null && viewModel != null)
			{
				if (e.Key == Key.Tab && viewModel.Suggestions.Count > 0)
				{
					// Appliquer la première suggestion
					int caretIndex = textBox.CaretIndex;
					string suggestion = viewModel.Suggestions[0];
					string currentText = textBox.Text.Substring(0, caretIndex);
					int lastSpaceIndex = currentText.LastIndexOf(' ');

					string newText;
					if (lastSpaceIndex >= 0)
					{
						newText = currentText.Substring(0, lastSpaceIndex + 1) + suggestion;
					}
					else
					{
						newText = suggestion;
					}

					textBox.Text = newText + textBox.Text.Substring(caretIndex);
					textBox.CaretIndex = newText.Length;
					e.Handled = true;
					_suggestionsPopup.IsOpen = false;
				}
				else if (e.Key == Key.Back || e.Key == Key.Delete)
				{
					// Mettre à jour les suggestions lors de la suppression de texte
					viewModel.CurrentInput = textBox.Text.Substring(0, textBox.CaretIndex);
					_suggestionsPopup.IsOpen = viewModel.Suggestions.Count > 0;
				}
				else
				{
					// Mettre à jour les suggestions lors de la saisie de texte
					if (textBox.Text != null)
					{
						viewModel.CurrentInput = textBox.Text.Substring(0, textBox.CaretIndex) + e.Key.ToString();
						_suggestionsPopup.IsOpen = viewModel.Suggestions.Count > 0;
					}
				}
			}
		}

		private void OnTitleBarPointerPressed(object sender, PointerPressedEventArgs e)
		{
			BeginMoveDrag(e);
		}

		private void OnMinimizeClick(object sender, RoutedEventArgs e)
		{
			WindowState = WindowState.Minimized;
		}

		private void OnMaximizeRestoreClick(object sender, RoutedEventArgs e)
		{
			WindowState = WindowState == WindowState.Maximized ? WindowState.Normal : WindowState.Maximized;
		}

		private void OnCloseClick(object sender, RoutedEventArgs e)
		{
			Close();
		}
	}

	public class MainWindowViewModel : ReactiveObject
	{
		private string _code;
		private string _output;
		private string _input;
		private string _lastOpenedFolder;
		private Process _runningProcess;
		private FileItem _selectedFile;
		private readonly Window _window;

		public MainWindowViewModel(Window window)
		{
			_window = window;
			OpenCommand = ReactiveCommand.Create(Open);
			SaveCommand = ReactiveCommand.Create(Save);
			UndoCommand = ReactiveCommand.Create(Undo);
			RedoCommand = ReactiveCommand.Create(Redo);
			RunCommand = ReactiveCommand.Create(Run);
			SendInputCommand = ReactiveCommand.Create(SendInput);
			OpenFolderCommand = ReactiveCommand.Create(OpenFolder);
			NewFileCommand = ReactiveCommand.Create(NewFile);
			NewFolderCommand = ReactiveCommand.Create(NewFolder);
			RenameCommand = ReactiveCommand.Create(Rename);
			RefreshCommand = ReactiveCommand.Create(RefreshFileTree);
			LoadLastOpenedFolder();
		}

		public string Code
		{
			get => _code;
			set => this.RaiseAndSetIfChanged(ref _code, value);
		}

		public string Output
		{
			get => _output;
			set => this.RaiseAndSetIfChanged(ref _output, value);
		}

		public string Input
		{
			get => _input;
			set => this.RaiseAndSetIfChanged(ref _input, value);
		}

		public string LastOpenedFolder
		{
			get => _lastOpenedFolder;
			set
			{
				this.RaiseAndSetIfChanged(ref _lastOpenedFolder, value);
				SaveLastOpenedFolder();
			}
		}

		public FileItem SelectedFile
		{
			get => _selectedFile;
			set
			{
				this.RaiseAndSetIfChanged(ref _selectedFile, value);
				if (value != null)
				{
					if (File.Exists(value.Path))
					{
						Code = File.ReadAllText(value.Path);
					}
					else
					{
						// Handle the error, e.g., show a message to the user
					}
				}
			}
		}

		private ObservableCollection<string> _suggestions = new ObservableCollection<string>();
		public ObservableCollection<string> Suggestions
		{
			get => _suggestions;
			set => this.RaiseAndSetIfChanged(ref _suggestions, value);
		}

		private string _currentInput;
		public string CurrentInput
		{
			get => _currentInput;
			set
			{
				this.RaiseAndSetIfChanged(ref _currentInput, value);
				UpdateSuggestions();
			}
		}

		public ObservableCollection<FileItem> FileTree { get; } = new ObservableCollection<FileItem>();

		public ReactiveCommand<Unit, Unit> OpenCommand { get; }
		public ReactiveCommand<Unit, Unit> SaveCommand { get; }
		public ReactiveCommand<Unit, Unit> UndoCommand { get; }
		public ReactiveCommand<Unit, Unit> RedoCommand { get; }
		public ReactiveCommand<Unit, Unit> RunCommand { get; }
		public ReactiveCommand<Unit, Unit> SendInputCommand { get; }
		public ReactiveCommand<Unit, Unit> OpenFolderCommand { get; }
		public ReactiveCommand<Unit, Unit> NewFileCommand { get; }
		public ReactiveCommand<Unit, Unit> NewFolderCommand { get; }
		public ReactiveCommand<Unit, Unit> RenameCommand { get; }
		public ReactiveCommand<Unit, Unit> RefreshCommand { get; }

		private void Open()
		{
			// Implémentez la logique pour ouvrir un fichier
		}

		private void Save()
		{
			if (SelectedFile != null && File.Exists(SelectedFile.Path))
			{
				File.WriteAllText(SelectedFile.Path, Code);
			}
		}

		private void Undo()
		{
			// Implémentez la logique pour annuler la dernière action
		}

		private void Redo()
		{
			// Implémentez la logique pour rétablir la dernière action annulée
		}

		private void UpdateSuggestions()
		{
			// Liste des suggestions pour DLang
			var allSuggestions = new List<string>
	{
		"void", "module", "import", "class", "interface", "struct", "enum", "union",
		"template", "mixin", "alias", "pragma", "version", "debug", "static", "const",
		"immutable", "shared", "abstract", "override", "final", "synchronized", "nothrow",
		"pure", "ref", "out", "in", "scope", "return", "break", "continue", "goto", "try",
		"catch", "finally", "throw", "new", "delete", "this", "super", "if", "else", "while",
		"do", "for", "foreach", "foreach_reverse", "switch", "case", "default", "with",
		"assert", "unittest", "import", "export", "extern", "align", "asm", "body", "delegate",
		"function", "inout", "invariant", "is", "lazy", "out", "scope", "sizeof", "typeof",
		"typeid", "typedef", "union", "version", "volatile", "wchar", "dchar", "string",
		"wstring", "dstring", "bool", "byte", "ubyte", "short", "ushort", "int", "uint",
		"long", "ulong", "float", "double", "real", "ifloat", "idouble", "ireal", "cfloat",
		"cdouble", "creal", "cent", "ucent"
	};

			Suggestions.Clear();
			var currentWord = GetCurrentWord(CurrentInput);
			foreach (var suggestion in allSuggestions)
			{
				if (suggestion.StartsWith(currentWord, StringComparison.OrdinalIgnoreCase))
				{
					Suggestions.Add(suggestion);
				}
			}
		}

		private string GetCurrentWord(string input)
		{
			if (string.IsNullOrEmpty(input))
				return string.Empty;

			var words = input.Split(' ');
			return words[^1]; // Retourne le dernier mot
		}


		private async void Run()
		{
			Output = "";
			string rootFolder = LastOpenedFolder;
			string dubPath = "dub"; // Assurez-vous que DUB est dans le PATH ou spécifiez le chemin complet
			string dubJsonPath = Path.Combine(rootFolder, "dub.json");
			string dubSdlPath = Path.Combine(rootFolder, "dub.sdl");

			// Créer un fichier dub.json ou dub.sdl s'il n'existe pas
			if (!File.Exists(dubJsonPath) && !File.Exists(dubSdlPath))
			{
				File.WriteAllText(dubJsonPath, GetDefaultDubJsonContent());
				RefreshFileTree();
			}

			ProcessStartInfo compileStartInfo = new ProcessStartInfo
			{
				FileName = dubPath,
				Arguments = "build",
				WorkingDirectory = rootFolder,
				RedirectStandardOutput = true,
				RedirectStandardError = true,
				UseShellExecute = false,
				CreateNoWindow = true
			};

			try
			{
				using (Process compileProcess = Process.Start(compileStartInfo))
				{
					compileProcess.OutputDataReceived += (sender, args) => Output += args.Data + "\n";
					compileProcess.ErrorDataReceived += (sender, args) => Output += args.Data + "\n";
					compileProcess.BeginOutputReadLine();
					compileProcess.BeginErrorReadLine();
					await compileProcess.WaitForExitAsync();

					if (compileProcess.ExitCode == 0)
					{
						// Compilation réussie, exécutez le programme dans une nouvelle fenêtre de console
						ProcessStartInfo runStartInfo = new ProcessStartInfo
						{
							FileName = dubPath,
							Arguments = "run",
							WorkingDirectory = rootFolder,
							UseShellExecute = true, // Utiliser une fenêtre de console séparée
							CreateNoWindow = false // Créer une fenêtre de console
						};

						Process.Start(runStartInfo);
					}
					else
					{
						Output += "Compilation failed.\n";
					}
				}
			}
			catch (FileNotFoundException)
			{
				// Redirigez l'utilisateur vers la page d'installation de DUB
				Process.Start(new ProcessStartInfo
				{
					FileName = "https://dub.pm/getting_started",
					UseShellExecute = true
				});
			}
		}

		private void SendInput()
		{
			if (_runningProcess != null && !_runningProcess.HasExited)
			{
				_runningProcess.StandardInput.WriteLine(Input);
				Input = string.Empty;
			}
		}

		private async void OpenFolder()
		{
			var result = await _window.StorageProvider.OpenFolderPickerAsync(new FolderPickerOpenOptions());

			if (result != null && result.Count > 0)
			{
				LastOpenedFolder = result[0].Path.LocalPath;
				LoadFileTree(LastOpenedFolder);
			}
		}

		private void LoadFileTree(string path)
		{
			FileTree.Clear();
			var root = new FileItem(path);
			FileTree.Add(root);
		}

		private void RefreshFileTree()
		{
			if (!string.IsNullOrEmpty(LastOpenedFolder))
			{
				LoadFileTree(LastOpenedFolder);
			}
		}

		private async void NewFile()
		{
			if (SelectedFile != null && Directory.Exists(SelectedFile.Path))
			{
				var newFileName = await ShowInputDialog("New File", "Enter the name of the new file:");
				if (!string.IsNullOrEmpty(newFileName))
				{
					string newFilePath = Path.Combine(SelectedFile.Path, newFileName);
					File.Create(newFilePath).Dispose();
					SelectedFile.Children.Add(new FileItem(newFilePath));
				}
			}
		}

		private async void NewFolder()
		{
			if (SelectedFile != null && Directory.Exists(SelectedFile.Path))
			{
				var newFolderName = await ShowInputDialog("New Folder", "Enter the name of the new folder:");
				if (!string.IsNullOrEmpty(newFolderName))
				{
					string newFolderPath = Path.Combine(SelectedFile.Path, newFolderName);
					Directory.CreateDirectory(newFolderPath);
					SelectedFile.Children.Add(new FileItem(newFolderPath));
				}
			}
		}

		private async void Rename()
		{
			if (SelectedFile != null)
			{
				var newName = await ShowInputDialog("Rename", "Enter the new name:");
				if (!string.IsNullOrEmpty(newName))
				{
					var newPath = Path.Combine(Path.GetDirectoryName(SelectedFile.Path), newName);

					if (File.Exists(SelectedFile.Path))
					{
						File.Move(SelectedFile.Path, newPath);
					}
					else if (Directory.Exists(SelectedFile.Path))
					{
						Directory.Move(SelectedFile.Path, newPath);
					}

					SelectedFile.Name = newName;
					SelectedFile.Path = newPath;
				}
			}
		}

		private void SaveLastOpenedFolder()
		{
			// Sauvegarder le dernier dossier ouvert dans un fichier de configuration
			File.WriteAllText("lastOpenedFolder.txt", LastOpenedFolder);
		}

		private void LoadLastOpenedFolder()
		{
			// Charger le dernier dossier ouvert à partir du fichier de configuration
			if (File.Exists("lastOpenedFolder.txt"))
			{
				LastOpenedFolder = File.ReadAllText("lastOpenedFolder.txt");
				LoadFileTree(LastOpenedFolder);
			}
		}

		public void OpenSelectedFile()
		{
			if (SelectedFile != null && File.Exists(SelectedFile.Path))
			{
				Code = File.ReadAllText(SelectedFile.Path);
			}
		}

		private async Task<string> ShowInputDialog(string title, string message)
		{
			var dialog = new InputDialog
			{
				Title = title
			};

			var viewModel = dialog.DataContext as InputDialogViewModel;
			viewModel.Message = message;

			var result = await dialog.ShowDialog<string>(_window);
			return result;
		}

		private string GetDefaultDubJsonContent()
		{
			return @"
{
    ""name"": ""my_project"",
    ""description"": ""A simple D project"",
    ""authors"": [
        ""Your Name""
    ],
    ""dependencies"": {
    },
    ""targetType"": ""executable""
}";
		}
	}


	public class FileItem : ReactiveObject
	{
		private string _name;
		private string _path;

		public string Name
		{
			get => _name;
			set => this.RaiseAndSetIfChanged(ref _name, value);
		}

		public string Path
		{
			get => _path;
			set => this.RaiseAndSetIfChanged(ref _path, value);
		}

		public bool IsDirectory => Directory.Exists(Path);

		public ObservableCollection<FileItem> Children { get; }

		public FileItem(string path)
		{
			Path = path;
			Name = System.IO.Path.GetFileName(path);
			Children = new ObservableCollection<FileItem>();

			if (Directory.Exists(path))
			{
				foreach (var dir in Directory.GetDirectories(path))
				{
					Children.Add(new FileItem(dir));
				}

				foreach (var file in Directory.GetFiles(path))
				{
					Children.Add(new FileItem(file));
				}
			}
		}
	}
}