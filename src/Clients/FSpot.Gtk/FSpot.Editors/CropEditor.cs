//
// CropEditor.cs
//
// Author:
//   Ruben Vermeersch <ruben@savanne.be>
//
// Copyright (C) 2008-2010 Novell, Inc.
// Copyright (C) 2008, 2010 Ruben Vermeersch
//
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Xml.Serialization;

using FSpot.Resources.Lang;
using FSpot.Settings;
using FSpot.UI.Dialog;
using FSpot.Utils;

using Gdk;

using Gtk;

namespace FSpot.Editors
{
	class CropEditor : Editor
	{
		TreeStore constraints_store;
		ComboBox constraints_combo;

		public enum ConstraintType
		{
			Normal,
			AddCustom,
			SameAsPhoto
		}

		List<SelectionRatioDialog.SelectionConstraint> custom_constraints;

		static SelectionRatioDialog.SelectionConstraint[] default_constraints = {
			new SelectionRatioDialog.SelectionConstraint (Strings.FourByThreeBook, 4.0 / 3.0),
			new SelectionRatioDialog.SelectionConstraint (Strings.FourBySixPostcard, 6.0 / 4.0),
			new SelectionRatioDialog.SelectionConstraint (Strings.FiveBySevenL2L, 7.0 / 5.0),
			new SelectionRatioDialog.SelectionConstraint (Strings.EightByTen, 10.0 / 8.0),
			new SelectionRatioDialog.SelectionConstraint (Strings.Square, 1.0)
		};

		public CropEditor () : base (Strings.Crop, "crop")
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
			case Preferences.CustomCropRatios:
				custom_constraints = new List<SelectionRatioDialog.SelectionConstraint> ();
				if (Preferences.Get<string[]> (key) != null) {
					var serializer = new XmlSerializer (typeof (SelectionRatioDialog.SelectionConstraint));
					foreach (string xml in Preferences.Get<string[]> (key))
						custom_constraints.Add ((SelectionRatioDialog.SelectionConstraint)serializer.Deserialize (new StringReader (xml)));
				}
				PopulateConstraints ();
				break;
			}
		}

		public override Widget ConfigurationWidget ()
		{
			var vbox = new VBox ();

			var info = new Label (Strings.SelectTheAreaThatNeedsCropping);

			constraints_combo = new ComboBox ();
			var constraint_name_cell = new CellRendererText ();
			var constraint_pix_cell = new CellRendererPixbuf ();
			constraints_combo.PackStart (constraint_name_cell, true);
			constraints_combo.PackStart (constraint_pix_cell, false);
			constraints_combo.SetCellDataFunc (constraint_name_cell, new CellLayoutDataFunc (ConstraintNameCellFunc));
			constraints_combo.SetCellDataFunc (constraint_pix_cell, new CellLayoutDataFunc (ConstraintPixCellFunc));
			constraints_combo.Changed += HandleConstraintsComboChanged;

			// FIXME: need tooltip Strings.ConstrainTheAspectRatioOfTheSelection

			LoadPreference (Preferences.CustomCropRatios);

			vbox.Add (info);
			vbox.Add (constraints_combo);

			return vbox;
		}

		void PopulateConstraints ()
		{
			constraints_store = new TreeStore (typeof (string), typeof (string), typeof (double), typeof (ConstraintType));
			constraints_combo.Model = constraints_store;
			constraints_store.AppendValues (null, Strings.NoConstraint, 0.0, ConstraintType.Normal);
			constraints_store.AppendValues (null, Strings.SameAsPhoto, 0.0, ConstraintType.SameAsPhoto);
			foreach (SelectionRatioDialog.SelectionConstraint constraint in custom_constraints)
				constraints_store.AppendValues (null, constraint.Label, constraint.XyRatio, ConstraintType.Normal);
			foreach (SelectionRatioDialog.SelectionConstraint constraint in default_constraints)
				constraints_store.AppendValues (null, constraint.Label, constraint.XyRatio, ConstraintType.Normal);
			constraints_store.AppendValues (Stock.Edit, Strings.CustomRatios, 0.0, ConstraintType.AddCustom);
			constraints_combo.Active = 0;
		}

		void UpdateSelectionCombo (object sender, EventArgs e)
		{
			if (!StateInitialized || constraints_combo == null)
				// Don't bomb out on instant-apply.
				return;

			//constraints_combo.Active = 0;
			if (constraints_combo.GetActiveIter (out var iter)) {
				if (((ConstraintType)constraints_store.GetValue (iter, 3)) == ConstraintType.SameAsPhoto)
					constraints_combo.Active = 0;
			}
		}

		void HandleConstraintsComboChanged (object o, EventArgs e)
		{
			if (State.PhotoImageView == null) {
				Logger.Log.Debug ("PhotoImageView is null");
				return;
			}

			if (constraints_combo.GetActiveIter (out var iter)) {
				double ratio = ((double)constraints_store.GetValue (iter, 2));
				var type = ((ConstraintType)constraints_store.GetValue (iter, 3));
				switch (type) {
				case ConstraintType.Normal:
					State.PhotoImageView.SelectionXyRatio = ratio;
					break;
				case ConstraintType.AddCustom:
					var dialog = new SelectionRatioDialog ();
					dialog.Run ();
					break;
				case ConstraintType.SameAsPhoto:
					try {
						Pixbuf pb = State.PhotoImageView.CompletePixbuf ();
						State.PhotoImageView.SelectionXyRatio = (double)pb.Width / (double)pb.Height;
					} catch (Exception ex) {
						Logger.Log.Warning ($"Exception in selection ratio's: {ex}");
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
				(cell as CellRendererPixbuf).Pixbuf = GtkUtil.TryLoadIcon (FSpotConfiguration.IconTheme, stockname, 16, (Gtk.IconLookupFlags)0);
			else
				(cell as CellRendererPixbuf).Pixbuf = null;
		}

		protected override Pixbuf Process (Pixbuf input, Cms.Profile input_profile)
		{
			Rectangle selection = FSpot.Utils.PixbufUtils.TransformOrientation ((int)State.PhotoImageView.PixbufOrientation <= 4 ? input.Width : input.Height,
												(int)State.PhotoImageView.PixbufOrientation <= 4 ? input.Height : input.Width,
												State.Selection, State.PhotoImageView.PixbufOrientation);
			var edited = new Pixbuf (input.Colorspace,
						 input.HasAlpha, input.BitsPerSample,
						 selection.Width, selection.Height);

			input.CopyArea (selection.X, selection.Y,
					selection.Width, selection.Height, edited, 0, 0);
			return edited;
		}
	}
}
