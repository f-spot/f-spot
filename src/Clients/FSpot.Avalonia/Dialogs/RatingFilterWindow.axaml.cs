// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using Avalonia.Automation;
using Avalonia.Controls;
using Avalonia.Interactivity;

using FSpot.Resources.Lang;

namespace FSpot.Avalonia.Dialogs;

/// <summary>
/// Find ▸ By Rating ▸ Set Rating Filter: Min and Max star pickers that keep min ≤ max.
/// </summary>
public partial class RatingFilterWindow : Window
{
	bool syncing;

	public RatingFilterWindow ()
		: this (RatingFilterLogic.DefaultMin, RatingFilterLogic.DefaultMax)
	{
	}

	public RatingFilterWindow (uint min, uint max)
	{
		InitializeComponent ();
		MinHeading.Text = DialogText.Plain (DialogStrings.BMinRatingB);
		MaxHeading.Text = DialogText.Plain (DialogStrings.BMaxRatingB);
		AutomationProperties.SetName (MinPicker, MinHeading.Text);
		AutomationProperties.SetName (MaxPicker, MaxHeading.Text);
		MinPicker.Value = min;
		MaxPicker.Value = max;
		MinPicker.ValueChanged += (_, _) => Sync (RatingFilterLogic.WithMin (MinPicker.Value, MaxPicker.Value));
		MaxPicker.ValueChanged += (_, _) => Sync (RatingFilterLogic.WithMax (MinPicker.Value, MaxPicker.Value));
		Opened += (_, _) => OkButton.Focus ();
	}

	public (uint Min, uint Max)? Result { get; private set; }

	internal uint Min => MinPicker.Value;
	internal uint Max => MaxPicker.Value;

	internal void SetMin (uint value) => MinPicker.Value = value;
	internal void SetMax (uint value) => MaxPicker.Value = value;

	void Sync ((uint Min, uint Max) range)
	{
		if (syncing)
			return;
		syncing = true;
		MinPicker.Value = range.Min;
		MaxPicker.Value = range.Max;
		syncing = false;
	}

	internal void Accept ()
	{
		Result = (MinPicker.Value, MaxPicker.Value);
		Close ();
	}

	void OnOk (object? sender, RoutedEventArgs e)
		=> Accept ();

	void OnCancel (object? sender, RoutedEventArgs e)
		=> Close ();
}
