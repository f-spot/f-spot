/*
 * LastRollDialog.cs:
 *
 * Author(s):
 * 	Bengt Thuree (bengt@thuree.com)
 *	Stephane Delcroix  <stephane@delcroix.org>
 *
 * This is free software. See COPYING for details.
 */


using System;
using Gtk;
using Gnome;
using FSpot.Query;
using FSpot.UI.Dialog;

namespace FSpot.UI.Dialog {
	public class LastRolls : GladeDialog {
		FSpot.PhotoQuery query;
		RollStore rollstore;
		Gtk.Window parent_window;
		
		Roll [] rolls;

		[Glade.Widget] private ComboBox combo_filter; // at, after, or between
		[Glade.Widget] private ComboBox combo_roll_1;
		[Glade.Widget] private ComboBox combo_roll_2;
		[Glade.Widget] private Label    and_label; // and label between two comboboxes.
		[Glade.Widget] private Label    photos_in_selected_rolls; 
		
		public LastRolls (FSpot.PhotoQuery query, RollStore rollstore, Gtk.Window parent_window) : base ("last_import_rolls_filter") 
		{
			this.query = query;
			this.rollstore = rollstore;
			this.parent_window = parent_window;
			rolls = rollstore.GetRolls (FSpot.Preferences.Get<int> (FSpot.Preferences.IMPORT_GUI_ROLL_HISTORY));

			PopulateCombos ();
			
			combo_filter.Active = 0;
			combo_roll_1.Active = 0;
			combo_roll_2.Active = 0;
			
			Dialog.DefaultResponse = ResponseType.Ok;
			Dialog.Response += HandleResponse;
			Dialog.Show ();
		}

		[GLib.ConnectBefore]
		protected void HandleResponse (object o, Gtk.ResponseArgs args)
		{
			if (args.ResponseId == ResponseType.Ok) {
				Roll [] selected_rolls = SelectedRolls ();
				
				if (selected_rolls.Length > 0 )
					query.RollSet = new RollSet (selected_rolls);
			}
			Dialog.Destroy ();
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

		private void UpdateNumberOfPhotos()
		{
			Roll [] selected_rolls = SelectedRolls ();
			uint sum = 0;
			if (selected_rolls != null)
				foreach (Roll roll in selected_rolls) 
					sum = sum + rollstore.PhotosInRoll (roll);
			photos_in_selected_rolls.Text = sum.ToString();
		}
		
		private void PopulateCombos ()
		{
			for (uint k = 0; k < rolls.Length; k++)
			{
				uint numphotos = rollstore.PhotosInRoll (rolls [k]);
				DateTime date = rolls [k].Time.ToLocalTime ();
				
				string header = String.Format ("{0} ({1})",
					date.ToString("%dd %MMM, %HH:%mm"),
					numphotos);		
					
				combo_roll_1.AppendText (header);
				combo_roll_2.AppendText (header);
			}					
		}
		
		private Roll [] SelectedRolls ()
		{
			if ((combo_roll_1.Active < 0) || ((combo_filter.Active == 2) && (combo_roll_2.Active < 0)))
				return null;
				
			System.Collections.ArrayList result = new System.Collections.ArrayList ();

			switch (combo_filter.Active) {
			case 0 : // at - Return the roll the user selected
				result.Add (rolls [combo_roll_1.Active]);
				break;
			case 1 : // after - Return all rolls from latest to the one the user selected
				for (uint k = 0; k <= combo_roll_1.Active; k++)
					result.Add (rolls [k]);
				break;
			case 2 : // between - Return all rolls between the two import rolls the user selected
				uint k1 = (uint) combo_roll_1.Active;
				uint k2 = (uint) combo_roll_2.Active;
				if (k1 > k2) {
					k1 = (uint) combo_roll_2.Active;
					k2 = (uint) combo_roll_1.Active;
				}
				for (uint k = k1; k <= k2; k++)
					result.Add (rolls[k]);
				break;
			}
			return (Roll []) result.ToArray (typeof(Roll));
		}
	}
}
