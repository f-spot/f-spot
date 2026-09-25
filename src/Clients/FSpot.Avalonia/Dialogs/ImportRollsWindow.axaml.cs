// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using System.Globalization;

using Avalonia.Automation;
using Avalonia.Controls;
using Avalonia.Interactivity;

using FSpot.Resources.Lang;

namespace FSpot.Avalonia.Dialogs;

/// <summary>
/// Find ▸ By Import Roll ▸ Select Import Rolls: view the photos imported at, after, or
/// between rolls, with the number of photos the choice covers.
/// </summary>
public partial class ImportRollsWindow : Window
{
	readonly IReadOnlyList<ImportRollOption> rolls;

	public ImportRollsWindow ()
		: this ([])
	{
	}

	public ImportRollsWindow (IReadOnlyList<ImportRollOption> rolls)
	{
		this.rolls = rolls;
		InitializeComponent ();
		Heading.Text = DialogText.Plain (DialogStrings.BViewAllPicturesImportedB);
		AutomationProperties.SetName (ModeBox, Heading.Text);
		AutomationProperties.SetName (FirstRollBox, Strings.ImportRoll);
		AutomationProperties.SetName (SecondRollBox, Strings.And);

		ModeBox.ItemsSource = new[] { DialogStrings.At, Strings.After, DialogStrings.Between };
		var labels = rolls.Select (roll => roll.Label).ToList ();
		FirstRollBox.ItemsSource = labels;
		SecondRollBox.ItemsSource = labels;

		ModeBox.SelectedIndex = 0;
		FirstRollBox.SelectedIndex = rolls.Count > 0 ? 0 : -1;
		SecondRollBox.SelectedIndex = rolls.Count > 0 ? 0 : -1;
		ModeBox.SelectionChanged += (_, _) => Refresh ();
		FirstRollBox.SelectionChanged += (_, _) => Refresh ();
		SecondRollBox.SelectionChanged += (_, _) => Refresh ();
		Refresh ();
	}

	public IReadOnlyList<uint>? Result { get; private set; }

	internal ImportRollMode Mode {
		get => (ImportRollMode)Math.Max (0, ModeBox.SelectedIndex);
		set => ModeBox.SelectedIndex = (int)value;
	}

	internal void Choose (int first, int second = 0)
	{
		FirstRollBox.SelectedIndex = first;
		SecondRollBox.SelectedIndex = second;
	}

	internal string CountText => CountLabel.Text ?? string.Empty;
	internal bool ShowsSecondRoll => SecondRollBox.IsVisible;

	IReadOnlyList<ImportRollOption> Selected ()
		=> ImportRollFilter.Select (rolls, Mode, FirstRollBox.SelectedIndex, SecondRollBox.SelectedIndex);

	void Refresh ()
	{
		bool between = Mode == ImportRollMode.Between;
		SecondRollBox.IsVisible = between;
		AndLabel.IsVisible = between;
		CountLabel.Text = ImportRollFilter.PhotoCount (Selected ()).ToString (CultureInfo.CurrentCulture);
	}

	internal void Accept ()
	{
		var selected = Selected ();
		Result = selected.Count > 0 ? selected.Select (roll => roll.Id).ToList () : null;
		Close ();
	}

	void OnOk (object? sender, RoutedEventArgs e)
		=> Accept ();

	void OnCancel (object? sender, RoutedEventArgs e)
		=> Close ();
}
