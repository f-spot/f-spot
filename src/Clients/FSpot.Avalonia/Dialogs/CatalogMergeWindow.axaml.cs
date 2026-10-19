// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using Avalonia.Automation;
using Avalonia.Controls;
using Avalonia.Interactivity;

using FSpot;
using FSpot.Avalonia;
using FSpot.Resources.Lang;

namespace FSpot.Avalonia.Dialogs;

public partial class CatalogMergeWindow : Window
{
	readonly IReadOnlyList<ImportRollOption> rolls;

	public CatalogMergeWindow ()
		: this ([])
	{
	}

	public CatalogMergeWindow (IReadOnlyList<ImportRollOption> rolls)
	{
		this.rolls = rolls ?? [];
		InitializeComponent ();
		Title = GtkMnemonic.Display (Strings.MergeAnotherFSpotCollection);
		AutomationProperties.SetName (this, Title);
		ImportLabel.Text = Strings.Import3;
		NewRolls.Content = Strings.NewRollsOnly;
		OneRoll.Content = Strings.ASingleImportRoll;
		AllRolls.Content = Strings.Everything;
		CopyFilesBox.Content = Strings.CopyImagesToPhotosFolder;
		KeepFiles.Content = Strings.KeepTheImagesAtTheirOriginalLocation;
		RollHelp.Text = DialogText.Plain (Strings.SmallIChooseWhatToImportFromTheSelectedDbNewRollsOnlyIsTheSmartOptionThatWillAvoidReImportingPhotosYouCouldHaveImportedDuringAPreviousOperationASingleImportRollLetYouChooseWhichRollYouWantToMergeBackEverythingWillImportEverythingCreatingDuplicatesIfYouAlreadyImportedFromThatDatabaseISmall);
		CopyHelp.Text = DialogText.Plain (Strings.SmallICopyTheImagesLocallyOrKeepThemWhereTheyAreIfYouChoseTheLaterBeSureThatThisLocationWillStayAccessibleFromFSpotISmall);
		RollBox.ItemsSource = this.rolls.Select (roll => roll.Label).ToList ();
		RollBox.SelectedIndex = this.rolls.Count > 0 ? 0 : -1;
		RollBox.IsEnabled = false;
		NewRolls.IsCheckedChanged += (_, _) => UpdateOk ();
		OneRoll.IsCheckedChanged += (_, _) => {
			RollBox.IsEnabled = Rolls == CatalogRollChoice.One;
			UpdateOk ();
		};
		AllRolls.IsCheckedChanged += (_, _) => UpdateOk ();
		RollBox.SelectionChanged += (_, _) => UpdateOk ();
		UpdateOk ();
		Opened += (_, _) => OkButton.Focus ();
	}

	public CatalogRollChoice Rolls =>
		OneRoll.IsChecked == true ? CatalogRollChoice.One :
		AllRolls.IsChecked == true ? CatalogRollChoice.All :
		CatalogRollChoice.New;

	public bool CopyFiles => KeepFiles.IsChecked != true;

	public bool RollListEnabled => RollBox.IsEnabled;

	public CatalogMergeChoice? Result { get; private set; }

	internal void ChooseRolls (CatalogRollChoice choice)
	{
		NewRolls.IsChecked = choice == CatalogRollChoice.New;
		OneRoll.IsChecked = choice == CatalogRollChoice.One;
		AllRolls.IsChecked = choice == CatalogRollChoice.All;
		RollBox.IsEnabled = choice == CatalogRollChoice.One;
		UpdateOk ();
	}

	internal void ChooseRoll (int index) => RollBox.SelectedIndex = index;

	internal void SetCopy (bool copy)
	{
		CopyFilesBox.IsChecked = copy;
		KeepFiles.IsChecked = !copy;
	}

	internal void Accept ()
	{
		// Roll id 0 means "not one chosen roll" for new rolls and everything.
		// A single roll with nothing selected must not accept that sentinel.
		uint id = 0;
		if (Rolls == CatalogRollChoice.One && !TrySelectedRoll (out id))
			return;
		Result = new CatalogMergeChoice (Rolls, CopyFiles, id);
		Close ();
	}

	void UpdateOk ()
		=> OkButton.IsEnabled = Rolls != CatalogRollChoice.One || TrySelectedRoll (out _);

	bool TrySelectedRoll (out uint id)
	{
		id = 0;
		if (RollBox.SelectedIndex < 0 || RollBox.SelectedIndex >= rolls.Count)
			return false;
		id = rolls[RollBox.SelectedIndex].Id;
		return true;
	}

	void OnOk (object? sender, RoutedEventArgs e)
		=> Accept ();

	void OnCancel (object? sender, RoutedEventArgs e)
		=> Close ();
}
