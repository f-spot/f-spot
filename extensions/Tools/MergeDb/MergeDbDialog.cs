/*
 * FSpot.MergeDbDialog.cs
 *
 * Author(s):
 *	Stephane Delcroix  <stephane@delcroix.org>
 *
 * This is free software. See COPYING for details
 */

using System;
using FSpot;
using FSpot.Query;

namespace MergeDbExtension
{
	internal class MergeDbDialog
	{
		[Glade.Widget] Gtk.Dialog mergedb_dialog;
		[Glade.Widget] Gtk.Button apply_button;
		[Glade.Widget] Gtk.FileChooserButton db_filechooser;
		[Glade.Widget] Gtk.RadioButton newrolls_radio;
		[Glade.Widget] Gtk.RadioButton allrolls_radio;
		[Glade.Widget] Gtk.RadioButton singleroll_radio;
		[Glade.Widget] Gtk.ComboBox rolls_combo;
		[Glade.Widget] Gtk.RadioButton copy_radio;
		[Glade.Widget] Gtk.RadioButton keep_radio;

		MergeDb parent;

		public event EventHandler FileSet;

		public MergeDbDialog (MergeDb parent) {
			this.parent = parent;

			Glade.XML xml = new Glade.XML (null, "MergeDb.glade", "mergedb_dialog", "f-spot");
			xml.Autoconnect (this);
			mergedb_dialog.Modal = false;
			mergedb_dialog.TransientFor = null;

			db_filechooser.LocalOnly = false;
			db_filechooser.FileSet += OnFileSet;

			newrolls_radio.Toggled += HandleRollsChanged;
			allrolls_radio.Toggled += HandleRollsChanged;
			singleroll_radio.Toggled += HandleRollsChanged;
		}

		void HandleRollsChanged (object o, EventArgs e)
		{
			rolls_combo.Sensitive = singleroll_radio.Active;
		}

		public Gtk.FileChooserButton FileChooser {
			get { return db_filechooser; }
		}

		Roll [] rolls;
		public Roll [] Rolls {
			get { return rolls; }
			set {
				rolls = value;
				foreach (Roll r in rolls) {
					uint numphotos = parent.FromDb.Rolls.PhotosInRoll (r);
					// Roll time is in UTC always
					DateTime date = r.Time.ToLocalTime ();
					rolls_combo.AppendText (String.Format ("{0} ({1})", date.ToString("%dd %MMM, %HH:%mm"), numphotos));
					rolls_combo.Active = 0;
				}
			}
		}

		public Roll [] ActiveRolls {
			get {
				if (allrolls_radio.Active)
					return null;
				if (newrolls_radio.Active)
					return rolls;
				else
					return new Roll [] {rolls [rolls_combo.Active]};
			}
		}

		public bool Copy {
			get { return copy_radio.Active; }
		}

		public void OnFileSet (object o, EventArgs e)
		{
			if (FileSet != null)
				FileSet (o, e);
		}

		public void SetSensitive ()
		{
			newrolls_radio.Sensitive = true;
			allrolls_radio.Sensitive = true;
			singleroll_radio.Sensitive = true;
			apply_button.Sensitive = true;
			copy_radio.Sensitive = true;
			keep_radio.Sensitive = true;
		}

		public Gtk.Dialog Dialog {
			get { return mergedb_dialog; }
		}

		public void ShowAll ()
		{
			mergedb_dialog.ShowAll ();
		}
	}
}
