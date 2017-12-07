//
// CropEditor.cs
//
// Author:
//   Ruben Vermeersch <ruben@savanne.be>
//
// Copyright (C) 2008-2010 Novell, Inc.
// Copyright (C) 2008, 2010 Ruben Vermeersch
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
using System.IO;
using System.Xml.Serialization;

using FSpot;
using FSpot.Settings;
using FSpot.UI.Dialog;
using FSpot.Utils;

using Hyena;

using Gdk;
using Gtk;

using Mono.Unix;

namespace FSpot.Editors
{
	class CropEditor : Editor
	{
		TreeStore constraints_store;
		ComboBox constraints_combo;

		public enum ConstraintType {
			Normal,
			AddCustom,
			SameAsPhoto
		}

		List<SelectionRatioDialog.SelectionConstraint> custom_constraints;

		static SelectionRatioDialog.SelectionConstraint [] default_constraints = {
			new SelectionRatioDialog.SelectionConstraint (Catalog.GetString ("4 x 3 (Book)"), 4.0 / 3.0),
			new SelectionRatioDialog.SelectionConstraint (Catalog.GetString ("4 x 6 (Postcard)"), 6.0 / 4.0),
			new SelectionRatioDialog.SelectionConstraint (Catalog.GetString ("5 x 7 (L, 2L)"), 7.0 / 5.0),
			new SelectionRatioDialog.SelectionConstraint (Catalog.GetString ("8 x 10"), 10.0 / 8.0),
			new SelectionRatioDialog.SelectionConstraint (Catalog.GetString ("Square"), 1.0)
		};

		public CropEditor () : base (Catalog.GetString ("Crop"), "crop")
		{
			NeedsSelection = true;

			Preferences.SettingChanged += OnPreferencesChanged;

			Initialized += delegate { State.PhotoImageView.PhotoChanged += UpdateSelectionCombo; };
		}

		void OnPreferencesChanged (object sender, NotifyEventArgs args)
		{
			LoadPreference (args.Key);
		}

		void LoadPreference (string key)
		{
			switch (key) {
			case Preferences.CUSTOM_CROP_RATIOS:
				custom_constraints = new List<SelectionRatioDialog.SelectionConstraint> ();
				if (Preferences.Get<string[]> (key) != null) {
					XmlSerializer serializer = new XmlSerializer (typeof(SelectionRatioDialog.SelectionConstraint));
					foreach (string xml in Preferences.Get<string[]> (key))
						custom_constraints.Add ((SelectionRatioDialog.SelectionConstraint)serializer.Deserialize (new StringReader (xml)));
				}
				PopulateConstraints ();
				break;
			}
		}

		public override Widget ConfigurationWidget ()
		{
			VBox vbox = new VBox ();

			Label info = new Label (Catalog.GetString ("Select the area that needs cropping."));

			constraints_combo = new ComboBox ();
			CellRendererText constraint_name_cell = new CellRendererText ();
			CellRendererPixbuf constraint_pix_cell = new CellRendererPixbuf ();
			constraints_combo.PackStart (constraint_name_cell, true);
			constraints_combo.PackStart (constraint_pix_cell, false);
			constraints_combo.SetCellDataFunc (constraint_name_cell, new CellLayoutDataFunc (ConstraintNameCellFunc));
			constraints_combo.SetCellDataFunc (constraint_pix_cell, new CellLayoutDataFunc (ConstraintPixCellFunc));
			constraints_combo.Changed += HandleConstraintsComboChanged;

			// FIXME: need tooltip Catalog.GetString ("Constrain the aspect ratio of the selection")

			LoadPreference (Preferences.CUSTOM_CROP_RATIOS);

			vbox.Add (info);
			vbox.Add (constraints_combo);

			return vbox;
		}

		void PopulateConstraints ()
		{
			constraints_store = new TreeStore (typeof (string), typeof (string), typeof (double), typeof (ConstraintType));
			constraints_combo.Model = constraints_store;
			constraints_store.AppendValues (null, Catalog.GetString ("No Constraint"), 0.0, ConstraintType.Normal);
			constraints_store.AppendValues (null, Catalog.GetString ("Same as photo"), 0.0, ConstraintType.SameAsPhoto);
			foreach (SelectionRatioDialog.SelectionConstraint constraint in custom_constraints)
				constraints_store.AppendValues (null, constraint.Label, constraint.XyRatio, ConstraintType.Normal);
			foreach (SelectionRatioDialog.SelectionConstraint constraint in default_constraints)
				constraints_store.AppendValues (null, constraint.Label, constraint.XyRatio, ConstraintType.Normal);
			constraints_store.AppendValues (Stock.Edit, Catalog.GetString ("Custom Ratios..."), 0.0, ConstraintType.AddCustom);
			constraints_combo.Active = 0;
		}

		void UpdateSelectionCombo (object sender, EventArgs e)
		{
			if (!StateInitialized || constraints_combo == null)
				// Don't bomb out on instant-apply.
				return;

			//constraints_combo.Active = 0;
			TreeIter iter;
			if (constraints_combo.GetActiveIter (out iter)) {
				if (((ConstraintType)constraints_store.GetValue (iter, 3)) == ConstraintType.SameAsPhoto)
					constraints_combo.Active = 0;
			}
		}

		void HandleConstraintsComboChanged (object o, EventArgs e)
		{
			if (State.PhotoImageView == null) {
				Log.Debug ("PhotoImageView is null");
				return;
			}

			TreeIter iter;
			if (constraints_combo.GetActiveIter (out iter)) {
				double ratio = ((double)constraints_store.GetValue (iter, 2));
				ConstraintType type = ((ConstraintType)constraints_store.GetValue (iter, 3));
				switch (type) {
				case ConstraintType.Normal:
					State.PhotoImageView.SelectionXyRatio = ratio;
					break;
				case ConstraintType.AddCustom:
					SelectionRatioDialog dialog = new SelectionRatioDialog ();
					dialog.Run ();
					break;
				case ConstraintType.SameAsPhoto:
					try {
						Pixbuf pb = State.PhotoImageView.CompletePixbuf ();
						State.PhotoImageView.SelectionXyRatio = (double)pb.Width / (double)pb.Height;
					} catch (System.Exception ex) {
						Log.WarningFormat ("Exception in selection ratio's: {0}", ex);
						State.PhotoImageView.SelectionXyRatio = 0;
					}
					break;
				default:
					State.PhotoImageView.SelectionXyRatio = 0;
					break;
				}
			}
		}

		void ConstraintNameCellFunc (CellLayout cell_layout, CellRenderer cell, TreeModel tree_model, TreeIter iter)
		{
			string name = (string)tree_model.GetValue (iter, 1);
			(cell as CellRendererText).Text = name;
		}

		void ConstraintPixCellFunc (CellLayout cell_layout, CellRenderer cell, TreeModel tree_model, TreeIter iter)
		{
			string stockname = (string)tree_model.GetValue (iter, 0);
			if (stockname != null)
				(cell as CellRendererPixbuf).Pixbuf = GtkUtil.TryLoadIcon (FSpot.Settings.Global.IconTheme, stockname, 16, (Gtk.IconLookupFlags)0);
			else
				(cell as CellRendererPixbuf).Pixbuf = null;
		}

		protected override Pixbuf Process (Pixbuf input, Cms.Profile input_profile)
		{
			Rectangle selection = FSpot.Utils.PixbufUtils.TransformOrientation ((int)State.PhotoImageView.PixbufOrientation <= 4 ? input.Width : input.Height,
											    (int)State.PhotoImageView.PixbufOrientation <= 4 ? input.Height : input.Width,
											    State.Selection, State.PhotoImageView.PixbufOrientation);
			Pixbuf edited = new Pixbuf (input.Colorspace,
						 input.HasAlpha, input.BitsPerSample,
						 selection.Width, selection.Height);

			input.CopyArea (selection.X, selection.Y,
					selection.Width, selection.Height, edited, 0, 0);
			return edited;
		}
	}
}
