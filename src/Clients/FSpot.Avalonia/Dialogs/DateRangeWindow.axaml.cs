// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using Avalonia.Automation;
using Avalonia.Controls;
using Avalonia.Interactivity;

using FSpot.Resources.Lang;

namespace FSpot.Avalonia.Dialogs;

/// <summary>
/// Find ▸ By Date ▸ Set Date Range: a period preset or a start and end date. Editing a
/// date switches the period to Customized Range; All Images disables the dates.
/// </summary>
public partial class DateRangeWindow : Window
{
	readonly DateTime today;
	bool syncing;

	public DateRangeWindow ()
		: this (DateTime.Today, null, null)
	{
	}

	public DateRangeWindow (DateTime today, DateTime? start, DateTime? end)
	{
		this.today = today.Date;
		InitializeComponent ();
		PeriodHeading.Text = DialogText.Plain (DialogStrings.BSelectPeriodB);
		StartHeading.Text = DialogText.Plain (DialogStrings.BStartDateB);
		EndHeading.Text = DialogText.Plain (DialogStrings.BEndDateB);
		AutomationProperties.SetName (PeriodBox, PeriodHeading.Text);
		AutomationProperties.SetName (StartPicker, StartHeading.Text);
		AutomationProperties.SetName (EndPicker, EndHeading.Text);

		PeriodBox.ItemsSource = DateRangePresets.All.Select (preset => DateRangePresets.Label (preset, this.today)).ToList ();
		PeriodBox.SelectionChanged += (_, _) => OnPeriodChanged ();
		StartPicker.PropertyChanged += OnPickerChanged;
		EndPicker.PropertyChanged += OnPickerChanged;

		Preset = DateRangePresets.Default;
		// Like GTK, an active filter's dates show in the pickers under the default period.
		if (start is DateTime from && end is DateTime to) {
			syncing = true;
			StartPicker.SelectedDate = from.Date;
			EndPicker.SelectedDate = to.Date;
			syncing = false;
		}
	}

	public DateRangeChoice? Result { get; private set; }

	internal DateRangePreset Preset {
		get => PeriodBox.SelectedIndex < 0 ? DateRangePresets.Default : DateRangePresets.All[PeriodBox.SelectedIndex];
		set => PeriodBox.SelectedIndex = IndexOf (value);
	}

	internal void SetDates (DateTime start, DateTime end)
	{
		StartPicker.SelectedDate = start;
		EndPicker.SelectedDate = end;
	}

	internal bool DatesEnabled => StartPicker.IsEnabled;

	static int IndexOf (DateRangePreset preset)
	{
		for (int i = 0; i < DateRangePresets.All.Count; i++) {
			if (DateRangePresets.All[i] == preset)
				return i;
		}
		return 0;
	}

	(DateTime Start, DateTime End)? CurrentRange ()
		=> DateRangePresets.Range (Preset, today,
			StartPicker.SelectedDate ?? today, EndPicker.SelectedDate ?? today);

	void OnPeriodChanged ()
	{
		if (syncing)
			return;
		bool enabled = Preset != DateRangePreset.AllDates;
		StartPicker.IsEnabled = enabled;
		EndPicker.IsEnabled = enabled;
		if (CurrentRange () is not { } range)
			return;
		syncing = true;
		StartPicker.SelectedDate = range.Start.Date;
		EndPicker.SelectedDate = range.End.Date;
		syncing = false;
	}

	void OnPickerChanged (object? sender, global::Avalonia.AvaloniaPropertyChangedEventArgs e)
	{
		if (e.Property == CalendarDatePicker.SelectedDateProperty)
			OnDateEdited ();
	}

	void OnDateEdited ()
	{
		if (syncing)
			return;
		syncing = true;
		Preset = DateRangePreset.CustomizedRange;
		syncing = false;
	}

	internal void Accept ()
	{
		Result = new DateRangeChoice (CurrentRange ());
		Close ();
	}

	void OnOk (object? sender, RoutedEventArgs e)
		=> Accept ();

	void OnCancel (object? sender, RoutedEventArgs e)
		=> Close ();
}
