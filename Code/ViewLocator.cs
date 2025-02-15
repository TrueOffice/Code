using System;
using Avalonia.Controls;
using Avalonia.Controls.Templates;
using Code.Views; // Corrig� pour r�f�rencer le bon espace de noms

namespace Code
{
	public class ViewLocator : IDataTemplate
	{
		public Control? Build(object? param)
		{
			if (param is null)
				return null;

			var name = param.GetType().FullName!.Replace("ViewModel", "View", StringComparison.Ordinal);
			var type = Type.GetType(name);

			if (type != null)
			{
				return (Control)Activator.CreateInstance(type)!;
			}

			return new TextBlock { Text = "Not Found: " + name };
		}

		public bool Match(object? data)
		{
			return data is MainWindowViewModel; // Corrig� pour r�f�rencer le bon type de ViewModel
		}
	}
}
