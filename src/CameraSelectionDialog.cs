using System;
using Gtk;
using Glade;
using LibGPhoto2;

public class CameraSelectionDialog
{
	
	[Widget] Gtk.Dialog camera_selection_dialog;
	[Widget] Gtk.Button OKButton;
	[Widget] Gtk.Button cancelButton;
	[Widget] Gtk.TreeView cameraList;
	
	private CameraList camlist;
	
	public CameraSelectionDialog(CameraList camera_list)
	{
		camlist = camera_list;
	}
	
	public int Run()
	{
		int return_value = -1;
		
		Glade.XML gui = Glade.XML.FromAssembly ("f-spot.glade", "camera_selection_dialog", null);
		gui.Autoconnect (this);
		
		cameraList.Selection.Mode = SelectionMode.Single;
		cameraList.AppendColumn("Camera", new CellRendererText(), "text", 0);
		cameraList.AppendColumn("Port", new CellRendererText(), "text", 1);
		
		ListStore tstore = new ListStore(typeof(string), typeof(string));
		for (int i = 0; i < camlist.Count(); i++)
		{
			tstore.AppendValues(camlist.GetName(i), camlist.GetValue(i));
		}
		
		cameraList.Model = tstore;
		
		ResponseType response = (ResponseType) camera_selection_dialog.Run();
		
		if (response == ResponseType.Ok && cameraList.Selection.CountSelectedRows() == 1)
		{
			TreeIter selected_camera;
			TreeModel model;
			cameraList.Selection.GetSelected(out model, out selected_camera);
			return_value = camlist.GetPosition((string)model.GetValue(selected_camera, 0), (string)model.GetValue(selected_camera, 1));
		}
		
		camera_selection_dialog.Destroy();
		
		return return_value;
	}
}
