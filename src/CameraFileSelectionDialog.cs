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
	Db db;
	ListStore preview_list_store;
	
	string[] saved_files;
	Tag[] selected_tags;
	bool cancelled;
	
	public CameraFileSelectionDialog (GPhotoCamera cam, Db datab)
	{
		camera = cam;
		db = datab;
		saved_files = null;
		selected_tags = null;
	}
	
	public Tag[] Tags {
		get {
			return selected_tags;
		}
	}
	
	public bool Run()
	{	
		CreateInterface ();
		
		bool allowed_to_exit = false;
		while (!allowed_to_exit) {

			if (!cancelled) {
				ResponseType response = (ResponseType) camera_file_selection_dialog.Run ();
			
				if (response == ResponseType.Ok) {
					allowed_to_exit = !SaveFiles ();
					
					if (!cancelled && allowed_to_exit)
						ImportFiles ();
				} else
					allowed_to_exit = true;

			}
		}
		
		camera_file_selection_dialog.Destroy ();
		
		return !cancelled;
	}
	
	private void CreateInterface ()
	{
		Glade.XML gui = Glade.XML.FromAssembly ("f-spot.glade", "camera_file_selection_dialog", null);
		gui.Autoconnect (this);
		
		file_tree.Selection.Mode = SelectionMode.Multiple;
		file_tree.AppendColumn ("Path", new CellRendererText (), "text", DirectoryColumn);
		file_tree.AppendColumn ("File", new CellRendererText (), "text", FileColumn);
		file_tree.AppendColumn ("Preview", new CellRendererPixbuf (), "pixbuf", PreviewColumn);
		file_tree.AppendColumn ("Index", new CellRendererText (), "text", IndexColumn).Visible = false;
		
		preview_list_store = new ListStore (typeof (string), typeof (string), 
						    typeof (Pixbuf), typeof (int));

		file_tree.Model = preview_list_store;
		
		copied_file_destination.Text = FSpot.Global.HomeDirectory;
		
		GetPreviews ();
	}
	
	private void GetPreviews()
	{
		ProgressDialog pdialog = new ProgressDialog (Mono.Posix.Catalog.GetString ("Downloading Previews"), 
							     ProgressDialog.CancelButtonType.Cancel, 
							     camera.FileList.Count, 
							     camera_file_selection_dialog);
		
		int index = 0;
		foreach (GPhotoCameraFile file in camera.FileList) {
			string msg = String.Format (Mono.Posix.Catalog.GetString ("Downloading Preview of {0}"), file.FileName);
			cancelled = pdialog.Update (msg);

			if (cancelled) 
				return;
			
			Pixbuf thumbscale = camera.GetPreviewPixbuf (file);
			preview_list_store.AppendValues (file.Directory, file.FileName, thumbscale, index);
			index++;
		}
		
		pdialog.Destroy ();
	}
	
	private bool SaveFiles ()
	{
		TreeSelection selection = file_tree.Selection;
		TreeModel model;
		TreePath[] selected_rows = selection.GetSelectedRows (out model);
	
		saved_files = new string [selected_rows.Length];
		
		ProgressDialog pdialog = new ProgressDialog (Mono.Posix.Catalog.GetString ("Saving Files..."), 
							     ProgressDialog.CancelButtonType.Cancel, 
							     selected_rows.Length, 
							     camera_file_selection_dialog);
		
		if (copied_file_destination.Text.Length == 0) {
			MessageDialog md = new MessageDialog (camera_file_selection_dialog, 
							      DialogFlags.DestroyWithParent, 
							      MessageType.Warning, 
							      ButtonsType.Ok, 
							      Mono.Posix.Catalog.GetString ("A destination directory must be chosen."));
			md.Run ();
			md.Destroy ();
			return true;
		}
		
		int copied_file_count = 0;
		int file_number_offset = 0;
		string directory = NormalizeDirectory (copied_file_destination.Text);
		string prefix = NormalizePrefix (prefix_entry.Text);
		string number;
		string extension;
		string filename = "";

		foreach (TreePath cur_row in selected_rows) {
			if (cancelled) 
				return false;
			
			TreeIter cur_iter;
			if (model.GetIter (out cur_iter, cur_row)) {
				int index = (int) model.GetValue (cur_iter, IndexColumn);
				GPhotoCameraFile cur_cam_file = (GPhotoCameraFile) camera.FileList [index];
				
				extension = System.IO.Path.GetExtension (cur_cam_file.FileName).ToLower();
				
				file_number_offset--;
				do {
					file_number_offset++;
					number = PadNumber (copied_file_count + file_number_offset);	
					filename = directory + prefix + number + extension;
				} while (File.Exists (filename));
				
				camera.SaveFile (index, filename);
				saved_files [copied_file_count] = filename;
				copied_file_count++;
			}
			
			string msg = String.Format (Mono.Posix.Catalog.GetString ("Saved File {0}"), filename);
		}
		
		pdialog.Destroy ();
		return false;
	}
	
	private void ImportFiles ()
	{
		if (saved_files != null && import_files_checkbox.Active) {
			ImportCommand command = new ImportCommand (null);
			command.ImportFromPaths (db.Photos, saved_files, selected_tags);
		}
	}
	
	void HandleSelectSaveDirectory (object sender, EventArgs args)
	{		
		FileSelection fs = new FileSelection (Mono.Posix.Catalog.GetString ("Select save directory..."));
		fs.FileList.Sensitive = false;
		int result = fs.Run ();
		
		if ((ResponseType)result == ResponseType.Ok) {
			TreeSelection dirselection = fs.DirList.Selection;
			TreeModel model;
			TreeIter iter;

			copied_file_destination.Text = NormalizeDirectory(fs.Filename);

			if (dirselection.GetSelected(out model, out iter)) {
				string subdirname = (string)model.GetValue(iter, 0);
				copied_file_destination.Text += subdirname;
			}
		}
			
		fs.Hide ();
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
	
	private string PadNumber (int number)
	{
		string string_num = number.ToString ();
		
		return string_num.PadLeft(4, '0');
	}
	
	private bool IsSeparator (char sep)
	{
		return (sep == System.IO.Path.DirectorySeparatorChar 
			|| sep == System.IO.Path.AltDirectorySeparatorChar 
			|| sep == System.IO.Path.VolumeSeparatorChar);
	}
	
	private string NormalizeDirectory (string dir)
	{
		char lastchar = dir [dir.Length - 1];
		if (!IsSeparator (lastchar))
			return dir + System.IO.Path.DirectorySeparatorChar;
		return dir;
	}
	
	private string NormalizePrefix (string pre)
	{
		if (pre.Length == 0) 
			return pre;

		if (pre [pre.Length - 1] != ' ') 
			return pre + ' ';

		return pre;
	}
}
