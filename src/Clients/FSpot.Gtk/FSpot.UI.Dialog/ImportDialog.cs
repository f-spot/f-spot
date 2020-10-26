//
// ImportDialog.cs
//
// Author:
//   Ruben Vermeersch <ruben@savanne.be>
//
// Copyright (C) 2010 Novell, Inc.
// Copyright (C) 2010 Ruben Vermeersch
//
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Threading.Tasks;

using FSpot.Import;
using FSpot.Settings;
using FSpot.Utils;
using FSpot.Widgets;

using Gtk;

using Hyena;

using Mono.Unix;

namespace FSpot.UI.Dialog
{
	public class ImportDialog : BuilderDialog
	{
#pragma warning disable 649
		[GtkBeans.Builder.Object] Button cancel_button;
		[GtkBeans.Builder.Object] Button import_button;
		[GtkBeans.Builder.Object] CheckButton copy_check;
		[GtkBeans.Builder.Object] CheckButton duplicate_check;
		[GtkBeans.Builder.Object] CheckButton recurse_check;
		[GtkBeans.Builder.Object] CheckButton remove_check;
		[GtkBeans.Builder.Object] CheckButton merge_raw_and_jpeg_check;
		[GtkBeans.Builder.Object] Button remove_warning_button;
		[GtkBeans.Builder.Object] ComboBox sources_combo;
		[GtkBeans.Builder.Object] HBox tagentry_box;
		[GtkBeans.Builder.Object] HPaned import_hpaned;
		[GtkBeans.Builder.Object] ProgressBar progress_bar;
		[GtkBeans.Builder.Object] ScrolledWindow icon_scrolled;
		[GtkBeans.Builder.Object] ScrolledWindow photo_scrolled;
		[GtkBeans.Builder.Object] Label attachtags_label;
#pragma warning restore 649

		static readonly string SelectFolderLabel = Catalog.GetString ("Choose Folder...");
		ImportDialogController Controller { get; set; }
		TreeStore Sources { get; set; }

		static readonly Dictionary<string, ImportSource> HistorySources = new Dictionary<string, ImportSource> ();

		PhotoImageView photoView;
		TagEntry tagEntry;

		public ImportDialog (ImportDialogController controller, Window parent) : base ("import.ui", "import_dialog")
		{
			Controller = controller;
			TransientFor = parent;
			BuildDialog ();

			ResetPreview ();
			LoadPreferences ();
			ScanSources ();
			ConnectEvents ();
		}

		void BuildDialog ()
		{
			WindowPosition = WindowPosition.CenterOnParent;

			photoView = new PhotoImageView (Controller.Photos);
			photo_scrolled.Add (photoView);
			photo_scrolled.SetSizeRequest (200, 200);
			photoView.Show ();

			GtkUtil.ModifyColors (photo_scrolled);
			GtkUtil.ModifyColors (photoView);

			using var tray = new BrowseablePointerGridView (photoView.Item) {
				DisplayTags = false
			};
			icon_scrolled.Add (tray);
			tray.Show ();

			progress_bar.Hide ();

			import_button.Sensitive = false;

			tagEntry = new TagEntry (App.Instance.Database.Tags, false);
			tagEntry.UpdateFromTagNames (Array.Empty<string> ());
			tagentry_box.Add (tagEntry);
			tagEntry.Show ();
			attachtags_label.MnemonicWidget = tagEntry;
		}

		void ResetPreview ()
		{
			photoView.Pixbuf = GtkUtil.TryLoadIcon (FSpotConfiguration.IconTheme, "FSpot", 128, 0);
			photoView.ZoomFit (false);
		}

		void LoadPreferences ()
		{
			if (Preferences.Get<int> (Preferences.ImportWindowWidth) > 0) {
				Resize (Preferences.Get<int> (Preferences.ImportWindowWidth), Preferences.Get<int> (Preferences.ImportWindowHeight));
			}

			if (Preferences.Get<int> (Preferences.ImportWindowPanePosition) > 0) {
				import_hpaned.Position = Preferences.Get<int> (Preferences.ImportWindowPanePosition);
			}

			var preferences = Controller.Preferences;
			copy_check.Active = preferences.CopyFiles;
			recurse_check.Active = preferences.RecurseSubdirectories;
			duplicate_check.Active = preferences.DuplicateDetect;
			remove_check.Active = preferences.RemoveOriginals;
			remove_check.Sensitive = copy_check.Active;
			remove_warning_button.Sensitive = copy_check.Active && remove_check.Active;
			merge_raw_and_jpeg_check.Active = preferences.MergeRawAndJpeg;
		}

		async Task ScanSources ()
		{
			// Populates the source combo box
			Sources = new TreeStore (typeof (ImportSource), typeof (string), typeof (string), typeof (bool));
			sources_combo.Model = Sources;
			sources_combo.RowSeparatorFunc = (m, i) => string.IsNullOrEmpty ((m.GetValue (i, 1) as string));

			using var render = new CellRendererPixbuf ();
			sources_combo.PackStart (render, false);
			sources_combo.SetAttributes (render, "icon-name", 2, "sensitive", 3);

			using var render2 = new CellRendererText ();
			sources_combo.PackStart (render2, true);
			sources_combo.SetAttributes (render2, "text", 1, "sensitive", 3);

			try {
				await PopulateSourceCombo (null);
				QueueDraw ();
			} catch (Exception) {
				// Swallow the exception if the import was cancelled / dialog was closed.
				if (Controller != null)
					throw;
			}
		}

		async Task PopulateSourceCombo (ImportSource sourceToActivate)
		{
			int activateIndex = 0;
			sources_combo.Changed -= OnSourceComboChanged;
			Sources.Clear ();
			Sources.AppendValues (null, Catalog.GetString ("Choose Import source..."), string.Empty, true);
			Sources.AppendValues (null, SelectFolderLabel, "folder", true);
			Sources.AppendValues (null, string.Empty, string.Empty);
			bool mountAdded = false;

			foreach (var source in Controller.Sources) {
				if (source == sourceToActivate)
					activateIndex = Sources.IterNChildren ();

				Sources.AppendValues (source, source.Name, source.IconName, true);
				mountAdded = true;
			}

			if (!mountAdded)
				Sources.AppendValues (null, Catalog.GetString ("(No Cameras Detected)"), string.Empty, false);

			if (HistorySources.Count > 0) {
				Sources.AppendValues (null, string.Empty, string.Empty);
				foreach (var source in HistorySources.Values) {
					if (source == sourceToActivate)
						activateIndex = Sources.IterNChildren ();

					Sources.AppendValues (source, source.Name, source.IconName, true);
				}
			}
			sources_combo.Changed += OnSourceComboChanged;
			sources_combo.Active = activateIndex;
		}

		void ConnectEvents ()
		{
			var preferences = Controller.Preferences;
			Controller.StatusEvent += OnControllerStatusEvent;
			Controller.ProgressUpdated += OnControllerProgressUpdated;

			copy_check.Toggled += (o, args) => {
				preferences.CopyFiles = copy_check.Active;
				remove_check.Sensitive = copy_check.Active;
				remove_warning_button.Sensitive = copy_check.Active && remove_check.Active;
			};

			recurse_check.Toggled += (o, args) => {
				preferences.RecurseSubdirectories = recurse_check.Active;
			};

			duplicate_check.Toggled += (o, args) => {
				preferences.DuplicateDetect = duplicate_check.Active;
			};

			remove_check.Toggled += (o, args) => {
				preferences.RemoveOriginals = remove_check.Active;
				remove_warning_button.Sensitive = copy_check.Active && remove_check.Active;
			};

			import_button.Clicked += (o, args) => StartImport ();
			cancel_button.Clicked += (o, args) => CancelImport ();

			remove_warning_button.Clicked += (o, args) => {
				using var dialog = new MessageDialog (this, DialogFlags.Modal, MessageType.Warning, ButtonsType.Ok, true,
						Catalog.GetString ("Checking this box will remove the imported photos from the camera after the import finished successfully.\n\nIt is generally recommended to backup your photos before removing them from the camera. <b>Use this option at your own risk!</b>")) {
					Title = Catalog.GetString ("Warning")
				};
				dialog.Response += (s, arg) => dialog.Destroy ();
				dialog.Run ();
			};

			merge_raw_and_jpeg_check.Toggled += (o, args) => {
				preferences.MergeRawAndJpeg = merge_raw_and_jpeg_check.Active;
			};

			Response += (o, args) => {
				if (args.ResponseId == ResponseType.DeleteEvent) {
					CancelImport ();
				}
			};
		}

		void ShowFolderSelector ()
		{
			using var fileChooser =
				new FileChooserDialog (Catalog.GetString ("Import"), this,
				FileChooserAction.SelectFolder, Stock.Cancel, ResponseType.Cancel,
				Stock.Open, ResponseType.Ok) {
					SelectMultiple = false,
					LocalOnly = false
				};

			int response = fileChooser.Run ();
			if ((ResponseType)response == ResponseType.Ok) {
				var uri = new SafeUri (fileChooser.Uri, true);
				SwitchToFolderSource (uri);
			}

			fileChooser.Destroy ();
		}

		public void SwitchToFolderSource (SafeUri uri)
		{
			if (!HistorySources.TryGetValue (uri, out var source)) {
				var name = uri.GetFilename ();
				source = new ImportSource (uri, name, "folder");
				HistorySources[uri] = source;
			}

			PopulateSourceCombo (source);
			Controller.ActiveSource = source;
		}

		int current_index = -1;
		void OnSourceComboChanged (object sender, EventArgs args)
		{
			// Prevent double firing.
			if (sources_combo.Active == current_index) {
				Log.Debug ("Skipping double fire!");
				return;
			}
			current_index = sources_combo.Active;

			sources_combo.GetActiveIter (out var iter);
			if (!(Sources.GetValue (iter, 0) is ImportSource source)) {
				var label = (string)Sources.GetValue (iter, 1);
				if (label == SelectFolderLabel) {
					ShowFolderSelector ();
					return;
				}
				sources_combo.Active = 0;
				return;
			}
			Controller.ActiveSource = source;
		}

		void OnControllerStatusEvent (ImportEvent evnt)
		{
			Log.Debug ($"Received controller event: {evnt}");

			switch (evnt) {
			case ImportEvent.SourceChanged:
				HideScanSpinner ();
				ResetPreview ();
				import_button.Sensitive = true;
				break;

			case ImportEvent.PhotoScanStarted:
				ShowScanSpinner ();
				break;

			case ImportEvent.PhotoScanFinished:
				HideScanSpinner ();
				break;

			case ImportEvent.ImportStarted:
				ShowImportProgress ();
				break;

			case ImportEvent.ImportFinished:
				ShowFailuresIfNeeded (Controller.FailedImports);
				Controller = null;
				Destroy ();
				break;

			case ImportEvent.ImportError:
				//FIXME
				break;
			}
		}

		void ShowFailuresIfNeeded (List<SafeUri> files)
		{
			if (Controller.FailedImports.Count == 0)
				return;

			using var dialog = new ImportFailureDialog (files);
			dialog.Show ();
		}

		void OnControllerProgressUpdated (int current, int total)
		{
			var importingLabel = Catalog.GetString ("Importing Photos: {0} of {1}...");
			ThreadAssist.ProxyToMain (() => {
				progress_bar.Text = string.Format (importingLabel, current, total);
				progress_bar.Fraction = (double)current / Math.Max (total, 1);
			});
		}

		void StartImport ()
		{
			Controller.AttachTags (tagEntry.GetTypedTagNames ());
			Controller.StartImport ();
			import_button.Sensitive = false;
			OptionsSensitive = false;
		}

		void CancelImport ()
		{
			Controller.CancelImport ();
			Destroy ();
		}

		System.Threading.Timer progressBarTimer;
		void ShowImportProgress ()
		{
			progress_bar.Text = Catalog.GetString ("Importing photos...");
			progress_bar.Show ();
		}

		void ShowScanSpinner ()
		{
			//FIXME Using a GtkSpinner would be nicer here.
			progress_bar.Text = Catalog.GetString ("Searching for photos... (You can already click Import to continue)");
			progress_bar.Show ();
			progressBarTimer = new System.Threading.Timer ((s) => {
				progress_bar.Pulse ();
			}, null, 40, 40);
		}

		void HideScanSpinner ()
		{
			progressBarTimer?.Dispose ();
			progress_bar.Hide ();
		}

		public bool OptionsSensitive {
			set {
				sources_combo.Sensitive = value;
				copy_check.Sensitive = value;
				recurse_check.Sensitive = value;
				duplicate_check.Sensitive = value;
				tagentry_box.Sensitive = value;
				merge_raw_and_jpeg_check.Sensitive = value;
			}
		}
	}
}
