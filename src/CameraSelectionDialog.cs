using System;
using Gtk;
using Glade;
using LibGPhoto2;
using Mono.Unix;
using FSpot.UI.Dialog;

namespace FSpot {
	public class CameraSelectionDialog : GladeDialog
	{
		[Widget] Gtk.Button OKButton;
		[Widget] Gtk.Button cancelButton;
		[Widget] Gtk.TreeView cameraList;
		
		private CameraList camlist;
		
		public CameraSelectionDialog (CameraList camera_list) 
		{
			camlist = camera_list;
		}
	
		public int Run ()
		{
			this.CreateDialog ("camera_selection_dialog");
			int return_value = -1;
			
			
			cameraList.Selection.Mode = SelectionMode.Single;
			cameraList.AppendColumn (Catalog.GetString ("Camera"), new CellRendererText (), "text", 0);
			cameraList.AppendColumn (Catalog.GetString ("Port"), new CellRendererText (), "text", 1);
			
			ListStore tstore = new ListStore (typeof (string), typeof (string));
			for (int i = 0; i < camlist.Count (); i++) {
				tstore.AppendValues (camlist.GetName (i), camlist.GetValue (i));
			}
			
			cameraList.Model = tstore;
			ResponseType response = (ResponseType) this.Dialog.Run ();
			
			if (response == ResponseType.Ok && cameraList.Selection.CountSelectedRows () == 1) {
				TreeIter selected_camera;
				TreeModel model;
				
				cameraList.Selection.GetSelected (out model, out selected_camera);
				return_value = camlist.GetPosition ((string)model.GetValue (selected_camera, 0), 
								    (string)model.GetValue (selected_camera, 1));
			}
		
			this.Dialog.Destroy ();
			
			return return_value;
		}
	}
}
