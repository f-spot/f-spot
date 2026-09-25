// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using Avalonia.Automation;
using Avalonia.Controls;
using Avalonia.Interactivity;

namespace FSpot.Avalonia.Dialogs;

/// <summary>
/// Create New Version / Rename Version: the name must be new to the photo, except that a
/// rename may keep the current name (which then changes nothing).
/// </summary>
public partial class VersionNameWindow : Window
{
	readonly VersionNameRequest request;
	readonly string currentName;
	readonly IReadOnlyCollection<string> existingNames;

	public VersionNameWindow ()
		: this (VersionNameRequest.Create, string.Empty, [])
	{
	}

	public VersionNameWindow (VersionNameRequest request, string currentName, IReadOnlyCollection<string> existingNames)
	{
		this.request = request;
		this.currentName = currentName;
		this.existingNames = existingNames;
		InitializeComponent ();
		Title = VersionNameRules.Title (request);
		AutomationProperties.SetName (this, Title);
		PromptLabel.Content = VersionNameRules.Prompt (request);
		AutomationProperties.SetName (NameBox, VersionNameRules.Prompt (request));
		if (request == VersionNameRequest.Rename)
			NameBox.Text = currentName;
		NameBox.PropertyChanged += (_, e) => {
			if (e.Property == TextBox.TextProperty)
				Update ();
		};
		Opened += (_, _) => {
			NameBox.Focus ();
			NameBox.SelectAll ();
		};
		Update ();
	}

	public string? Result { get; private set; }

	internal string VersionName {
		get => NameBox.Text ?? string.Empty;
		set => NameBox.Text = value;
	}

	internal bool CanAccept => OkButton.IsEnabled;
	internal bool ShowsNameInUse => InUseLabel.IsVisible;

	void Update ()
	{
		InUseLabel.IsVisible = VersionNameRules.InUse (request, VersionName, currentName, existingNames);
		OkButton.IsEnabled = VersionNameRules.CanAccept (request, VersionName, currentName, existingNames);
	}

	internal void Accept ()
	{
		if (!VersionNameRules.CanAccept (request, VersionName, currentName, existingNames))
			return;
		Result = VersionNameRules.Result (request, VersionName, currentName);
		Close ();
	}

	void OnOk (object? sender, RoutedEventArgs e)
		=> Accept ();

	void OnCancel (object? sender, RoutedEventArgs e)
		=> Close ();
}
