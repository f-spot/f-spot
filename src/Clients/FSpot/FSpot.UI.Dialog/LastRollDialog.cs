//
// LastRollDialog.cs
//
// Author:
//   Ruben Vermeersch <ruben@savanne.be>
//   Stephane Delcroix <sdelcroix@src.gnome.org>
//   Stephen Shaw <sshaw@decriptor.com>
//
// Copyright (C) 2007-2010 Novell, Inc.
// Copyright (C) 2010 Ruben Vermeersch
// Copyright (C) 2007-2008 Stephane Delcroix
//
// Permission is hereby granted, free of charge, to any person obtaining
// a copy of this software and associated documentation files (the
// "Software"), to deal in the Software without restriction, including
// without limitation the rights to use, copy, modify, merge, publish,
// distribute, sublicense, and/or sell copies of the Software, and to
// permit persons to whom the Software is furnished to do so, subject to
// the following conditions:
//
// The above copyright notice and this permission notice shall be
// included in all copies or substantial portions of the Software.
//
// THE SOFTWARE IS PROVIDED AS IS, WITHOUT WARRANTY OF ANY KIND,
// EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF
// MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND
// NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE
// LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION
// OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION
// WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
//

using System;
using System.Collections.Generic;

using Gtk;

using FSpot.Core;
using FSpot.Database;
using FSpot.Query;
using FSpot.Settings;

namespace FSpot.UI.Dialog
{
	public class LastRolls : BuilderDialog
	{
		FSpot.PhotoQuery query;
		RollStore rollstore;
		Roll[] rolls;

#pragma warning disable 649
		[GtkBeans.Builder.Object] ComboBox combo_filter; // at, after, or between
		[GtkBeans.Builder.Object] ComboBox combo_roll_1;
		[GtkBeans.Builder.Object] ComboBox combo_roll_2;
		[GtkBeans.Builder.Object] Label and_label; // and label between two comboboxes.
		[GtkBeans.Builder.Object] Label photos_in_selected_rolls;
#pragma warning restore 649

		public LastRolls (FSpot.PhotoQuery query, RollStore rollstore, Window parent) : base ("LastImportRollFilterDialog.ui", "last_import_rolls_filter")
		{
			this.query = query;
			this.rollstore = rollstore;
			rolls = rollstore.GetRolls (Preferences.Get<int> (Preferences.IMPORT_GUI_ROLL_HISTORY));

			TransientFor = parent;

			PopulateCombos ();

			combo_filter.Active = 0;
			combo_roll_1.Active = 0;
			combo_roll_2.Active = 0;

			DefaultResponse = ResponseType.Ok;
			Response += HandleResponse;
			Show ();
		}

		[GLib.ConnectBefore]
		protected void HandleResponse (object o, Gtk.ResponseArgs args)
		{
			if (args.ResponseId == ResponseType.Ok) {
				Roll [] selected_rolls = SelectedRolls ();

				if (selected_rolls != null && selected_rolls.Length > 0)
					query.RollSet = new RollSet (selected_rolls);
			}
			Destroy ();
		}

		void HandleComboFilterChanged (object o, EventArgs args)
		{
			combo_roll_2.Visible = (combo_filter.Active == 2);
			and_label.Visible = combo_roll_2.Visible;

			UpdateNumberOfPhotos ();
		}

		void HandleComboRollChanged (object o, EventArgs args)
		{
			UpdateNumberOfPhotos ();
		}

		void UpdateNumberOfPhotos ()
		{
			Roll [] selected_rolls = SelectedRolls ();
			uint sum = 0;
			if (selected_rolls != null)
				foreach (Roll roll in selected_rolls) {
					sum = sum + rollstore.PhotosInRoll (roll);
				}
			photos_in_selected_rolls.Text = sum.ToString ();
		}

		void PopulateCombos ()
		{
			for (uint k = 0; k < rolls.Length; k++) {
				uint numphotos = rollstore.PhotosInRoll (rolls [k]);
				// Roll time is in UTC always
				DateTime date = rolls [k].Time.ToLocalTime ();

				string header = string.Format ("{0} ({1})",
					date.ToString ("%dd %MMM, %HH:%mm"),
					numphotos);

				combo_roll_1.AppendText (header);
				combo_roll_2.AppendText (header);
			}
		}

		Roll [] SelectedRolls ()
		{
			if ((combo_roll_1.Active < 0) || ((combo_filter.Active == 2) && (combo_roll_2.Active < 0)))
				return null;

			List<Roll> result = new List<Roll> ();

			switch (combo_filter.Active) {
			case 0 : // at - Return the roll the user selected
				result.Add (rolls [combo_roll_1.Active]);
				break;
			case 1 : // after - Return all rolls from latest to the one the user selected
				for (uint k = 0; k <= combo_roll_1.Active; k++) {
					result.Add (rolls [k]);
				}
				break;
			case 2 : // between - Return all rolls between the two import rolls the user selected
				uint k1 = (uint)combo_roll_1.Active;
				uint k2 = (uint)combo_roll_2.Active;
				if (k1 > k2) {
					k1 = (uint)combo_roll_2.Active;
					k2 = (uint)combo_roll_1.Active;
				}
				for (uint k = k1; k <= k2; k++) {
					result.Add (rolls [k]);
				}
				break;
			}
			return result.ToArray ();
		}
	}
}
