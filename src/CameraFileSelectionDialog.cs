using System;
using System.IO;
using Gdk;
using Gtk;
using Glade;
using LibGPhoto2;

public class CameraFileSelectionDialog
{
	private const int DirectoryColumn = 0;
	private const int FileColumn = 1;
	private const int PreviewColumn = 2;
	private const int IndexColumn = 3;
	
	[Widget] Gtk.Dialog camera_file_selection_dialog;
	[Widget] Gtk.Button copyButton;
	[Widget] Gtk.Button cancelButton;
	[Widget] Gtk.Label selected_camera_name;
	[Widget] Gtk.TreeView file_tree;
	[Widget] Gtk.Entry copied_file_destination;
	[Widget] Gtk.Button save_directory_selection_button;
	[Widget] Gtk.Entry prefix_entry;
	[Widget] Gtk.CheckButton import_files_checkbox;
	[Widget] Gtk.Button select_tag_button;
	
	GPhotoCamera camera;
	ListStore preview_list_store;
	Db db;

	FSpot.ThreadProgressDialog progress_dialog;
	System.Collections.ArrayList index_list;

	string[] saved_files;
	Tag[] selected_tags;
	
	string destination;
	System.Threading.Thread command_thread;
	
	public CameraFileSelectionDialog (GPhotoCamera cam, Db datab)
	{
		camera = cam;
		db = datab;
		saved_files = null;
		selected_tags = null;
	}
	
	public void Run ()
	{	
		CreateInterface ();
		
		ResponseType response = (ResponseType) camera_file_selection_dialog.Run ();
		if (response == ResponseType.Ok)
			if (SaveFiles ())
				ImportFiles ();

		camera_file_selection_dialog.Destroy ();
		return;
	}
	
	private void CreateInterface ()
	{
		Glade.XML gui = Glade.XML.FromAssembly ("f-spot.glade", "camera_file_selection_dialog", null);
		gui.Autoconnect (this);
		
		file_tree.Selection.Mode = SelectionMode.Multiple;
		file_tree.AppendColumn (Mono.Posix.Catalog.GetString ("Preview"), 
					new CellRendererPixbuf (), "pixbuf", PreviewColumn);
		file_tree.AppendColumn (Mono.Posix.Catalog.GetString ("Path"), 
					new CellRendererText (), "text", DirectoryColumn);
		file_tree.AppendColumn (Mono.Posix.Catalog.GetString ("File"), 
					new CellRendererText (), "text", FileColumn);
		file_tree.AppendColumn (Mono.Posix.Catalog.GetString ("Index"),
					new CellRendererText (), "text", IndexColumn).Visible = false;
		
		preview_list_store = new ListStore (typeof (string), typeof (string), 
						    typeof (Pixbuf), typeof (int));

		file_tree.Model = preview_list_store;
		
		copied_file_destination.Text = FSpot.Global.HomeDirectory;
		
		GetPreviews ();
	}
	
	private void GetPreviews()
	{
		lock (camera) {
			ProgressDialog pdialog = new ProgressDialog (Mono.Posix.Catalog.GetString ("Downloading Previews"), 
								     ProgressDialog.CancelButtonType.Cancel, 
								     camera.FileList.Count, 
								     camera_file_selection_dialog);
			
			int index = 0;
			foreach (GPhotoCameraFile file in camera.FileList) {
				string msg = String.Format (Mono.Posix.Catalog.GetString ("Downloading Preview of {0}"), 
							    file.FileName);
				
				if (pdialog.Update (msg)) {
					pdialog.Destroy ();
					return;
				}
				
				Pixbuf thumbscale = camera.GetPreviewPixbuf (file);
				Pixbuf scale = PixbufUtils.ScaleToMaxSize (thumbscale, 64,64);
				preview_list_store.AppendValues (file.Directory, file.FileName, scale, index);
				thumbscale.Dispose ();
				index++;
			}
			
			file_tree.Selection.SelectAll ();
			pdialog.Destroy ();
		}
	}

	private System.Collections.ArrayList GetSelectedItems ()
	{
		TreeSelection selection = file_tree.Selection;
		TreeModel model;
		TreePath[] selected_rows = selection.GetSelectedRows (out model);
	
		System.Collections.ArrayList list = new System.Collections.ArrayList ();
		foreach (TreePath cur_row in selected_rows) {
			TreeIter cur_iter;

			if (model.GetIter (out cur_iter, cur_row))
				list.Add ((int) model.GetValue (cur_iter, IndexColumn));
		}
		
		return list;
	}

	private bool PrepareDestination ()
	{
		if (copied_file_destination.Text.Length == 0) {
			HigMessageDialog md = new HigMessageDialog (camera_file_selection_dialog, 
								    DialogFlags.DestroyWithParent, 
								    MessageType.Warning, 
								    ButtonsType.Ok, 
								    Mono.Posix.Catalog.GetString ("Unknown destination."),
								    Mono.Posix.Catalog.GetString ("When copying files from a camera you must select a valid destination on the local filesystem"));
			md.Run ();
			md.Destroy ();

			return true;
		}
		
		destination = copied_file_destination.Text;

		if (!System.IO.Directory.Exists (destination)) {
			// FIXME ask for confimation
			try {
				System.IO.Directory.CreateDirectory (destination);
			} catch (System.Exception e) {
				HigMessageDialog md = new HigMessageDialog (camera_file_selection_dialog,
									    DialogFlags.DestroyWithParent,
									    MessageType.Error,
									    ButtonsType.Ok,
									    Mono.Posix.Catalog.GetString ("Unable to create directory."),
									    String.Format (Mono.Posix.Catalog.GetString ("Error \"{0}\" while creating directory \"{0}\".  Check that the path and permissions are correct and try again"), e.Message, destination));
				md.Run ();
				md.Destroy ();

				return true;
			}
		}

		return false;
	}
	

	private bool SaveFiles ()
	{
		if (PrepareDestination ())
			return false;
		
		index_list = GetSelectedItems ();
		camera_file_selection_dialog.Hide ();

		command_thread = new System.Threading.Thread (new System.Threading.ThreadStart (this.Download));
		command_thread.Name = Mono.Posix.Catalog.GetString ("Transferring Pictures");

		progress_dialog = new FSpot.ThreadProgressDialog (command_thread, 1);
		progress_dialog.Start ();

		while (command_thread.IsAlive) {
			if (Application.EventsPending ())
				Application.RunIteration ();

		}

		return true;
	}

	private void Download ()
	{
		lock (camera) {
			try {
				System.Collections.ArrayList saved = new System.Collections.ArrayList ();
				
				int count = 0;
				foreach (int index in index_list) {
					count++;
					string msg = String.Format (Mono.Posix.Catalog.GetString ("Copying file {0} of {1}"),
								    count, index_list.Count);

					progress_dialog.ProgressText = msg;
					saved.Add (SaveFile (index));
					progress_dialog.Fraction = count/(double)index_list.Count;
				}
				
				saved_files = (string []) saved.ToArray (typeof (string));

				progress_dialog.Message = Mono.Posix.Catalog.GetString ("Done Copying Files");
				progress_dialog.Fraction = 1.0;
				progress_dialog.ProgressText = Mono.Posix.Catalog.GetString ("Download Complete");
				progress_dialog.ButtonLabel = Gtk.Stock.Ok;
			} catch (System.Exception e) {
				System.Console.WriteLine (e.ToString ());
				progress_dialog.Message = String.Format ("{0}\n{1}", e.Message, e.ToString ());
				progress_dialog.ProgressText = Mono.Posix.Catalog.GetString ("Error transferring file");
			}
		}
	}
	
	private string SaveFile (int index) 
	{
		GPhotoCameraFile camfile = (GPhotoCameraFile) camera.FileList [index];
		string orig = System.IO.Path.Combine (destination, camfile.FileName.ToLower ());
		string path = orig;
		
		int i = 0;
		while (File.Exists (path)) {
			string name = String.Format ("{0}-{1}{2}", 
						     System.IO.Path.GetFileNameWithoutExtension (orig), 
						     i, System.IO.Path.GetExtension (orig));

			path = System.IO.Path.Combine (System.IO.Path.GetDirectoryName (orig), name);
			i++;
		}

		string msg = String.Format (Mono.Posix.Catalog.GetString ("Transferring \"{0}\" from camera"), 
					    System.IO.Path.GetFileName (path));
		progress_dialog.Message = msg;

		camera.SaveFile (index, path);
		return path;
	}

	private void ImportFiles ()
	{
		System.Console.WriteLine ("Calling Import");

		if (saved_files != null && import_files_checkbox.Active) {
			ImportCommand command = new ImportCommand (null);
			command.ImportFromPaths (db.Photos, saved_files, selected_tags);
		}
	}
	
	void HandleSelectSaveDirectory (object sender, EventArgs args)
	{		
		CompatFileChooserDialog file_selector =
			new CompatFileChooserDialog (Mono.Posix.Catalog.GetString ("Select Destination"), 
						     camera_file_selection_dialog,
						     CompatFileChooserDialog.Action.SelectFolder);

		file_selector.Filename = copied_file_destination.Text;
		int result = file_selector.Run ();

		if ((ResponseType)result == ResponseType.Ok)
			copied_file_destination.Text = file_selector.Filename;
			
		file_selector.Destroy ();
	}
	
	void HandleSelectTags (object sender, EventArgs args)
	{
		TagSelectionDialog tag_selection = new TagSelectionDialog (db.Tags);
		selected_tags = tag_selection.Run ();
		tag_selection.Hide (); 
	}
	
	void HandleImportToggled (object sender, EventArgs args)
	{
		if (sender is Gtk.CheckButton)
			select_tag_button.Sensitive = (sender as Gtk.CheckButton).Active;
	}

	public Tag[] Tags {
		get {
			return selected_tags;
		}
	}
}
