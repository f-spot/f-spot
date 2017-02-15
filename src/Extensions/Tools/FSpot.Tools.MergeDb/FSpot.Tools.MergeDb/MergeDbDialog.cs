//
// MergeDbDialog.cs
//
// Author:
//   Stephane Delcroix <sdelcroix*novell.com>
//   Paul Lange <palango@gmx.de>
//
// Copyright (C) 2008-2010 Novell, Inc.
// Copyright (C) 2008 Stephane Delcroix
// Copyright (C) 2010 Paul Lange
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

using FSpot.Core;

namespace FSpot.Tools.MergeDb
{
	internal class MergeDbDialog
	{
		[GtkBeans.Builder.Object] Gtk.Dialog mergedb_dialog;
		[GtkBeans.Builder.Object] Gtk.Button apply_button;
		[GtkBeans.Builder.Object] Gtk.FileChooserButton db_filechooser;
		[GtkBeans.Builder.Object] Gtk.RadioButton newrolls_radio;
		[GtkBeans.Builder.Object] Gtk.RadioButton allrolls_radio;
		[GtkBeans.Builder.Object] Gtk.RadioButton singleroll_radio;
		[GtkBeans.Builder.Object] Gtk.ComboBox rolls_combo;
		[GtkBeans.Builder.Object] Gtk.RadioButton copy_radio;
		[GtkBeans.Builder.Object] Gtk.RadioButton keep_radio;

		MergeDb parent;

		public event EventHandler FileSet;

		public MergeDbDialog (MergeDb parent) {
			this.parent = parent;

			var builder = new GtkBeans.Builder (null, "mergedb_dialog.ui", null);
			builder.Autoconnect (this);
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
					rolls_combo.AppendText (string.Format ("{0} ({1})", date.ToString("%dd %MMM, %HH:%mm"), numphotos));
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
