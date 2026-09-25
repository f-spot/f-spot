//
// SelectionRatioEditor.cs
//
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using System.Collections.ObjectModel;
using System.Globalization;

using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

using FSpot.Editors;
using FSpot.Resources.Lang;

namespace FSpot.Avalonia.ViewModels;

// One editable row of GTK's SelectionRatioDialog tree view.
public partial class SelectionRatioRow : ObservableObject
{
	[ObservableProperty]
	string label;

	[ObservableProperty]
	string ratioText;

	public SelectionRatioRow (string label, double ratio)
	{
		this.label = label;
		Ratio = ratio;
		ratioText = Format (ratio);
	}

	public double Ratio { get; private set; }

	// HandleRatioEdited: an unparsable edit is rejected and the old value kept.
	partial void OnRatioTextChanged (string value)
	{
		if (CropConstraints.TryParseRatio (value, out double ratio))
			Ratio = ratio;
	}

	public void CommitRatioText ()
		=> RatioText = Format (Ratio);

	static string Format (double ratio)
		=> ratio.ToString (CultureInfo.CurrentCulture);
}

// Model behind SelectionRatioWindow ("Selection Constraints").
public partial class SelectionRatioEditor : ObservableObject
{
	[ObservableProperty]
	[NotifyCanExecuteChangedFor (nameof (DeleteCommand), nameof (MoveUpCommand), nameof (MoveDownCommand))]
	SelectionRatioRow? selected;

	public SelectionRatioEditor (IEnumerable<CropConstraint> custom)
	{
		foreach (var constraint in custom)
			Rows.Add (new SelectionRatioRow (constraint.Label, constraint.XyRatio));
	}

	public ObservableCollection<SelectionRatioRow> Rows { get; } = new ();

	[RelayCommand]
	void Add ()
	{
		var row = new SelectionRatioRow (Strings.NewSelection, 1.0);
		Rows.Add (row);
		Selected = row;
	}

	bool HasSelection () => Selected is not null;

	[RelayCommand (CanExecute = nameof (HasSelection))]
	void Delete ()
	{
		if (Selected is null)
			return;
		Rows.Remove (Selected);
		Selected = null;
	}

	[RelayCommand (CanExecute = nameof (HasSelection))]
	void MoveUp ()
	{
		var row = Selected;
		int index = row is null ? -1 : Rows.IndexOf (row);
		if (index <= 0)
			return;
		Rows.Move (index, index - 1);
		Selected = row;
	}

	[RelayCommand (CanExecute = nameof (HasSelection))]
	void MoveDown ()
	{
		var row = Selected;
		int index = row is null ? -1 : Rows.IndexOf (row);
		if (index < 0 || index >= Rows.Count - 1)
			return;
		Rows.Move (index, index + 1);
		Selected = row;
	}

	public IReadOnlyList<CropConstraint> ToConstraints ()
		=> Rows.Select (row => new CropConstraint (row.Label, row.Ratio)).ToList ();
}
