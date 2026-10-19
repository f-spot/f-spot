// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using Avalonia.Automation;
using Avalonia.Controls;
using Avalonia.Interactivity;

using FSpot.Avalonia;
using FSpot.Resources.Lang;

namespace FSpot.Avalonia.Dialogs;

public partial class ZipScaleWindow : Window
{
	public ZipScaleWindow ()
	{
		InitializeComponent ();
		Title = GtkMnemonic.Display (Strings.CompressedFile);
		AutomationProperties.SetName (this, Title);
		string scale = GtkMnemonic.Display (Strings.Scale);
		ScaleBox.Content = scale;
		AutomationProperties.SetName (ScaleBox, scale);
		GtkMnemonic.ApplyHotKey (ScaleBox, Strings.Scale);
		string edge = GtkMnemonic.Display (Strings.ScalePhotosToNoLargerThan);
		EdgeLabel.Text = edge;
		AutomationProperties.SetName (Edge, edge);
		Edge.Value = ZipScaleChoice.DefaultLongerSide;
		Edge.IsEnabled = false;
		ScaleBox.IsCheckedChanged += (_, _) => Edge.IsEnabled = Scale;
		Opened += (_, _) => OkButton.Focus ();
	}

	public bool Scale => ScaleBox.IsChecked == true;

	public int LongerSide => (int)(Edge.Value ?? ZipScaleChoice.DefaultLongerSide);

	public ZipScaleChoice? Result { get; private set; }

	internal void SetScale (bool value) => ScaleBox.IsChecked = value;

	internal void SetLongerSide (int value) => Edge.Value = value;

	internal void Accept ()
	{
		int edge = LongerSide;
		if (edge < ZipScaleChoice.MinimumLongerSide)
			edge = ZipScaleChoice.MinimumLongerSide;
		if (edge > ZipScaleChoice.MaximumLongerSide)
			edge = ZipScaleChoice.MaximumLongerSide;
		Result = new ZipScaleChoice (Scale, edge);
		Close ();
	}

	void OnOk (object? sender, RoutedEventArgs e)
		=> Accept ();

	void OnCancel (object? sender, RoutedEventArgs e)
		=> Close ();
}
