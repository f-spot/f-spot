// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using Avalonia.Automation;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Media.Imaging;

using FSpot.Avalonia.Imaging;
using FSpot.Core;
using FSpot.Imaging;
using FSpot.Resources.Lang;

namespace FSpot.Avalonia.Dialogs;

/// <summary>
/// The GTK Create Tag and Edit Tag dialogs: a tag name that must not already be in use,
/// a parent tag, and either "Create icon when first used" (create) or an icon button
/// that opens Edit Tag Icon (edit).
/// </summary>
public partial class TagWindow : Window
{
	readonly Category root;
	readonly IReadOnlyList<TagParentOption> parents;
	readonly TagIconSource? editing;
	readonly string? originalName;
	Action? displayRefresh;

	public TagWindow ()
		: this (new Category (null, 0, string.Empty), null, null, true)
	{
	}

	TagWindow (Category root, Category? parent, TagIconSource? editing, bool autoIcon)
	{
		this.root = root;
		this.editing = editing;
		originalName = editing?.Tag.Name;
		InitializeComponent ();

		parents = TagDialogRules.ParentChoices (root, editing?.Tag);
		ParentBox.ItemsSource = parents;
		ParentBox.SelectedIndex = TagDialogRules.IndexOf (parents, editing?.Tag.Category ?? parent);

		string title;
		if (editing is null) {
			title = Strings.CreateNewTag;
			NameLabel.Content = DialogStrings.TagName2;
			OkButton.Content = DialogStrings.Create2;
			AutoIconBox.IsVisible = true;
			AutoIconBox.IsChecked = autoIcon;
			AutomationProperties.SetAutomationId (this, DialogAutomationIds.CreateTagWindow);
		} else {
			title = Strings.EditTag2;
			OkButton.Content = Strings.Ok;
			IconLabel.IsVisible = true;
			IconButton.IsVisible = true;
			NameBox.Text = editing.Tag.Name;
			AutomationProperties.SetAutomationId (this, DialogAutomationIds.EditTagWindow);
			ShowIcon ();
		}
		Title = title;
		AutomationProperties.SetName (this, title);
		AutomationProperties.SetName (OkButton, GtkMnemonic.Display (OkButton.Content as string));
		AutomationProperties.SetName (ParentBox, GtkMnemonic.Display (DialogStrings.ParentTag));
		NameBox.PropertyChanged += (_, e) => {
			if (e.Property == TextBox.TextProperty)
				Update ();
		};
		Opened += (_, _) => {
			NameBox.Focus ();
			NameBox.SelectAll ();
			if (displayRefresh == null) {
				displayRefresh = ShowIcon;
				DisplayProfileWatch.Attach (displayRefresh);
			}
		};
		Closed += (_, _) => {
			if (displayRefresh != null) {
				DisplayProfileWatch.Detach (displayRefresh);
				displayRefresh = null;
			}
			var icon = IconImage.Source as Bitmap;
			IconImage.Source = null;
			ReleasedBitmap.DisposePrevious (icon, null);
		};
		Update ();
	}

	public static TagWindow ForCreate (Category root, Category parent, bool autoIcon)
		=> new (root, parent, null, autoIcon);

	public static TagWindow ForEdit (Category root, TagIconSource tag)
		=> new (root, null, tag, false);

	public CreateTagChoice? CreateResult { get; private set; }
	public EditTagChoice? EditResult { get; private set; }

	/// <summary>
	/// The icon chosen in Edit Tag Icon, or null to leave the tag's icon alone.
	/// </summary>
	internal TagIconChoice? PendingIcon { get; set; }

	internal string TagName {
		get => NameBox.Text ?? string.Empty;
		set => NameBox.Text = value;
	}

	internal bool CanAccept => OkButton.IsEnabled;
	internal bool ShowsNameInUse => InUseLabel.IsVisible;
	internal IReadOnlyList<TagParentOption> ParentOptions => parents;

	internal Category SelectedParent {
		get => ParentBox.SelectedItem is TagParentOption option ? option.Category : root;
		set => ParentBox.SelectedIndex = TagDialogRules.IndexOf (parents, value);
	}

	void Update ()
	{
		string name = TagName;
		InUseLabel.IsVisible = TagDialogRules.NameInUse (root, name, originalName);
		OkButton.IsEnabled = TagDialogRules.CanAccept (root, name, originalName);
	}

	void ShowIcon ()
	{
		string? theme = PendingIcon is null ? editing?.Tag.ThemeIconName : PendingIcon.ThemeIconName;
		ImageBuffer? buffer = null;
		if (theme is null)
			buffer = PendingIcon is null ? CurrentIcon () : PendingIcon.Icon;
		IconSvg.Path = theme is null ? null : TagIcons.UriFor (theme);
		IconSvg.IsVisible = theme is not null;
		var shown = buffer is null ? null : AvaloniaImagingBackend.ToBgraBitmap (DisplayColor.Apply (buffer, DisplayColor.Selected ()));
		var previous = IconImage.Source as Bitmap;
		IconImage.Source = shown;
		ReleasedBitmap.DisposePrevious (previous, shown);
		IconImage.IsVisible = buffer is not null;
	}

	ImageBuffer? CurrentIcon ()
	{
		try {
			return editing?.Tag.Icon;
		} catch (InvalidOperationException) {
			return null;
		}
	}

	void OnEditIcon (object? sender, RoutedEventArgs e)
		=> UiTasks.Forget (EditIconAsync ());

	internal async Task EditIconAsync ()
	{
		if (editing is null)
			return;
		try {
			var dialog = new EditTagIconWindow (editing, PendingIcon);
			await dialog.ShowDialog (this);
			if (dialog.Result is null)
				return;
			PendingIcon = dialog.Result;
			ShowIcon ();
		} catch (Exception ex) {
			Logger.Log.Error (ex, "Failed to edit the tag icon");
		}
	}

	internal void Accept ()
	{
		if (!TagDialogRules.CanAccept (root, TagName, originalName))
			return;
		if (editing is null)
			CreateResult = new CreateTagChoice (TagName, SelectedParent, AutoIconBox.IsChecked == true);
		else
			EditResult = new EditTagChoice (TagName, SelectedParent, PendingIcon);
		Close ();
	}

	void OnOk (object? sender, RoutedEventArgs e)
		=> Accept ();

	void OnCancel (object? sender, RoutedEventArgs e)
		=> Close ();
}
