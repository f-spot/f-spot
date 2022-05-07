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
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;

using FSpot.Core;
using FSpot.Models;

namespace FSpot.Tools.MergeDb
{
	class MergeDbDialog
	{
#pragma warning disable 649
		[GtkBeans.Builder.Object] Gtk.Dialog mergedb_dialog;
		[GtkBeans.Builder.Object] Gtk.Button apply_button;
		[GtkBeans.Builder.Object] Gtk.FileChooserButton db_filechooser;
		[GtkBeans.Builder.Object] Gtk.RadioButton newrolls_radio;
		[GtkBeans.Builder.Object] Gtk.RadioButton allrolls_radio;
		[GtkBeans.Builder.Object] Gtk.RadioButton singleroll_radio;
		[GtkBeans.Builder.Object] Gtk.ComboBox rolls_combo;
		[GtkBeans.Builder.Object] Gtk.RadioButton copy_radio;
		[GtkBeans.Builder.Object] Gtk.RadioButton keep_radio;
#pragma warning restore 649

		MergeDb parent;

		public event EventHandler FileSet;

		public MergeDbDialog (MergeDb parent)
		{
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

		List<Roll> rolls;
		public List<Roll> Rolls {
			get { return rolls; }
			set {
				rolls = value;
				foreach (Roll r in rolls) {
					var numphotos = parent.FromDb.Rolls.PhotosInRoll (r);
					// Roll time is in UTC always
					DateTime date = r.UtcTime.ToLocalTime ();
					rolls_combo.AppendText ($"{date:%dd %MMM, %HH:%mm} ({numphotos})");
					rolls_combo.Active = 0;
				}
			}
		}

		public List<Roll> ActiveRolls {
			get {
				if (allrolls_radio.Active)
					return null;
				if (newrolls_radio.Active)
					return rolls;
				else
					return new List<Roll> { rolls[rolls_combo.Active] };
			}
		}

		public bool Copy {
			get { return copy_radio.Active; }
		}

		public void OnFileSet (object o, EventArgs e)
		{
			FileSet?.Invoke (o, e);
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
