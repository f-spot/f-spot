using Gdk;
using Gtk;
using System.Collections;
using System.Collections.Generic;
using System;
using FSpot;
using FSpot.Utils;
using FSpot.Xmp;
using FSpot.UI.Dialog;
using System.IO;
using Mono.Unix;
using Hyena;

public class FileImportBackend : ImportBackend {
	PhotoStore store;
	RollStore rolls = FSpot.App.Instance.Database.Rolls;
	TagStore tag_store = FSpot.App.Instance.Database.Tags;
	bool recurse;
	bool copy;
	bool detect_duplicates;
	SafeUri [] base_paths;
	Tag [] tags;
	Gtk.Window parent;

	bool cancel = false;
	int count;
	int duplicate_count;
	XmpTagsImporter xmptags;

	List<IBrowsableItem> import_info;
	Stack directories;

	public override List<IBrowsableItem> Prepare ()
	{
		if (import_info != null)
			throw new ImportException ("Busy");

		import_info = new List<IBrowsableItem> ();

		foreach (var uri in base_paths) {
			var enumerator = new RecursiveFileEnumerator (uri, recurse, true);
			foreach (var file in enumerator) {
				if (FSpot.ImageFile.HasLoader (new SafeUri (file.Uri, true)))
					import_info.Add (new ImportInfo (new SafeUri(file.Uri, true)));
			}
		}	

		directories = new Stack ();
		xmptags = new XmpTagsImporter (store, tag_store);

		roll = rolls.Create ();
		Photo.ResetMD5Cache ();

		return import_info;
	}
	

	public override bool Step (out StepStatusInfo status_info)
	{
		Photo photo = null;
		bool is_duplicate = false;

		if (import_info == null)
			throw new ImportException ("Prepare() was not called");

		if (this.count == import_info.Count)
			throw new ImportException ("Already finished");

		// FIXME Need to get the EXIF info etc.
		ImportInfo info = (ImportInfo)import_info [this.count];
		bool needs_commit = false;
		bool abort = false;
		try {
			var destination = info.DefaultVersion.Uri;
			if (copy)
				destination = ChooseLocation (info.DefaultVersion.Uri, directories);

			// Don't copy if we are already home
			if (info.DefaultVersion.Uri == destination) {
				info.DestinationUri = destination;

				if (detect_duplicates)
					photo = null;//store.CheckForDuplicate (destination);

				if (photo == null)
					photo = store.Create (info.DestinationUri, roll.Id);
				else
				 	is_duplicate = true;
			} else {
				var file = GLib.FileFactory.NewForUri (info.DefaultVersion.Uri);
				var new_file = GLib.FileFactory.NewForUri (destination);
				file.Copy (new_file, GLib.FileCopyFlags.AllMetadata, null, null);
				info.DestinationUri = destination;

				if (detect_duplicates)
					photo = null;// store.CheckForDuplicate (destination);

				if (photo == null)
				{
					photo = store.Create (info.DestinationUri,
					                      info.DefaultVersion.Uri,
					                      roll.Id);
				}
				else
				{
					is_duplicate = true; 
					new_file.Delete (null);
				}
			} 

			if (!is_duplicate)
			{
				if (tags != null) {
					foreach (Tag t in tags) {
						photo.AddTag (t);
					}
					needs_commit = true;
				}

				needs_commit |= xmptags.Import (photo, info.DestinationUri, info.DefaultVersion.Uri);

				if (needs_commit)
					store.Commit(photo);

                info.PhotoId = photo.Id;
			}
		} catch (System.Exception e) {
			Log.ErrorFormat ("Error importing {0}{2}{1}", info.DestinationUri.ToString (), e.ToString (), Environment.NewLine);
			photo = null;

			HigMessageDialog errordialog = new HigMessageDialog (parent,
									     Gtk.DialogFlags.Modal | Gtk.DialogFlags.DestroyWithParent,
									     Gtk.MessageType.Error,
									     Gtk.ButtonsType.Cancel,
									     Catalog.GetString ("Import error"),
									     String.Format(Catalog.GetString ("Error importing {0}{2}{2}{1}"), info.DefaultVersion.Uri.ToString (), e.Message, Environment.NewLine ));
			errordialog.AddButton (Catalog.GetString ("Skip"), Gtk.ResponseType.Reject, false);
			ResponseType response = (ResponseType) errordialog.Run ();
			errordialog.Destroy ();
			if (response == ResponseType.Cancel)
				abort = true;
		}

		this.count ++;

		if (is_duplicate)
			this.duplicate_count ++;

		status_info = new StepStatusInfo (photo, this.count, is_duplicate);

		if (cancel) {
			abort = true;
		}

		return (!abort && count != import_info.Count);
	}

	public override void Cancel ()
	{
		if (import_info == null)
			throw new ImportException ("Not doing anything");

		foreach (var item in import_info) {
            ImportInfo info = item as ImportInfo;
			
			if (info.DefaultVersion.Uri != info.DestinationUri && info.DestinationUri != null) {
				try {
					var file = GLib.FileFactory.NewForUri (info.DestinationUri);
					if (file.QueryExists (null))
						file.Delete (null);
				} catch (System.ArgumentNullException) {
					// Do nothing, since if DestinationUri == null, we do not have to remove it
				} catch (System.Exception e) {
					Log.Exception (e);
				}
			}
			
			if (info.PhotoId != 0)
				store.Remove (store.Get (info.PhotoId));
		}
		
		// clean up all the directories we created.
		if (copy) {
			string path;
			System.IO.DirectoryInfo info;
			while (directories.Count > 0) {
				path = directories.Pop () as string;
				info = new System.IO.DirectoryInfo (path);
				// double check we aren't trying to delete a directory that still contains something!
				if (info.Exists && info.GetFiles().Length == 0 && info.GetDirectories().Length == 0)
					info.Delete ();
			}
		}
		// Clean up just created tags
		xmptags.Cancel();

		rolls.Remove (roll);
	}

	public override void Finish ()
	{
		if (import_info == null)
			throw new ImportException ("Not doing anything");

		var infos = import_info;
		ThreadAssist.SpawnFromMain (() => {
			// Generate all thumbnails on a different thread, disposing is automatic.
			var loader = ThumbnailLoader.Default;
			foreach (ImportInfo info in infos) {
				if (info.PhotoId != 0) {
					var uri = store.Get (info.PhotoId).DefaultVersion.Uri;
					loader.Request (uri, ThumbnailSize.Large, 10);
				}
			}
		});

		import_info = null;
		xmptags.Finish();
		Photo.ResetMD5Cache ();

		if (count == duplicate_count)
			rolls.Remove (roll);

		count = duplicate_count = 0;
		//rolls.EndImport();    // Clean up the imported session.
	}
