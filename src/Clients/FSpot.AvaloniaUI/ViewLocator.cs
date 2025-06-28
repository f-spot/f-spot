using Avalonia.Controls;
using Avalonia.Controls.Templates;
using FSpot.AvaloniaUI.ViewModels;
using System;

namespace FSpot.AvaloniaUI;

public class ViewLocator : IDataTemplate
{
    public Control Build(object? data)
    {
        if (data is null)
            return new TextBlock { Text = "Data is null" };

        var name = data.GetType().FullName!.Replace("ViewModel", "View", StringComparison.Ordinal);
        var type = Type.GetType(name);

        if (type != null)
        {
            var control = (Control)Activator.CreateInstance(type)!;
            return control;
        }

        return new TextBlock { Text = $"Not Found: {name}" };
    }

    public bool Match(object? data)
    {
        return data is ViewModelBase;
    }
}