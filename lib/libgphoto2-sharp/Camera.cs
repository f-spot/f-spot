/*
 * Camera.cs
 *
 * Author(s):
 *	Ewen Cheslack-Postava <echeslack@gmail.com>
 *	Larry Ewing <lewing@novell.com>
 *	Stephane Delcroix <stephane@delcroix.org>
 *
 * Copyright (c) 2005-2009 Novell, Inc.
 *
 * This is open source software. See COPYING for details.
 */
using System;
using System.Runtime.InteropServices;

namespace GPhoto2
{

	public class Camera : GPObject 
	{
		[DllImport ("libgphoto2.so")]
		internal static extern ErrorCode gp_camera_new (out IntPtr handle);
		[DllImport ("libgphoto2.so")]
		internal static extern ErrorCode gp_camera_unref (HandleRef camera);

		public Camera () : base (gp_camera_unref)
		{
			IntPtr native;
			Error.CheckError (gp_camera_new (out native));
			this.handle = new HandleRef (this, native);
		}

#region Preparing initilization
		[DllImport ("libgphoto2.so")]
		internal static extern ErrorCode gp_camera_set_abilities (HandleRef camera, CameraAbilities abilities);

		[DllImport ("libgphoto2.so")]
		internal unsafe static extern ErrorCode gp_camera_get_abilities (HandleRef camera, out CameraAbilities abilities);

		public CameraAbilities Abilities {
			get {
				CameraAbilities abilities;
				Error.CheckError (gp_camera_get_abilities(this.Handle, out abilities));
				return abilities;
			}
			set { Error.CheckError (gp_camera_set_abilities(this.Handle, value)); }
		}

		[DllImport ("libgphoto2.so")]
		internal unsafe static extern ErrorCode gp_camera_set_port_info (HandleRef camera, PortInfo info);

		[DllImport ("libgphoto2.so")]
		internal static extern ErrorCode gp_camera_get_port_info (HandleRef camera, out PortInfo info);

		public PortInfo PortInfo {
			get {
				PortInfo portinfo;
				Error.CheckError (gp_camera_get_port_info (this.Handle, out portinfo));
				return portinfo;	
			}
			set { Error.CheckError (gp_camera_set_port_info (this.Handle, value)); }
		}
#endregion

#region Speed, do not use, camera driver pick the optimal one
		[DllImport ("libgphoto2.so")]
		internal static extern ErrorCode gp_camera_get_port_speed (HandleRef camera);

		[DllImport ("libgphoto2.so")]
		internal static extern ErrorCode gp_camera_set_port_speed (HandleRef camera, int speed);

		public int PortSpeed {
			get { return Error.CheckError (gp_camera_get_port_speed (this.Handle)); }
			set { Error.CheckError (gp_camera_set_port_speed (this.Handle, value)); }
		}
#endregion

#region Initialization
		[DllImport ("libgphoto2.so")]
		internal static extern ErrorCode gp_camera_init (HandleRef camera, HandleRef context);

		public void Init (Context context)
		{
			Error.CheckError (gp_camera_init (Handle, context.Handle));
		}
		
		[DllImport ("libgphoto2.so")]
		internal static extern ErrorCode gp_camera_exit (HandleRef camera, HandleRef context);
		
		public void Exit (Context context)
		{
			Error.CheckError (gp_camera_exit (Handle, context.Handle));
		}
#endregion

#region Operations on camera
		[DllImport ("libgphoto2.so")]
		internal static extern ErrorCode gp_camera_get_summary (HandleRef camera, out CameraText summary, HandleRef context);
		
		public CameraText GetSummary (Context context)
		{
			CameraText summary;
			Error.CheckError (Camera.gp_camera_get_summary(this.Handle, out summary, context.Handle));

			return summary;
		}
		
		[DllImport ("libgphoto2.so")]
		internal static extern ErrorCode gp_camera_get_about (HandleRef camera, out CameraText about, HandleRef context);

		public CameraText GetAbout (Context context)
		{
			CameraText about;
			Error.CheckError (gp_camera_get_about(this.Handle, out about, context.Handle));

			return about;
		}

		[DllImport ("libgphoto2.so")]
		internal static extern ErrorCode gp_camera_get_manual (HandleRef camera, out CameraText manual, HandleRef context);
		
		public CameraText GetManual (Context context)
		{
			CameraText manual;
			unsafe
			{
				Error.CheckError (gp_camera_get_manual(this.Handle, out manual, context.Handle));
			}
			return manual;
		}

		[DllImport ("libgphoto2.so")]
		internal static extern ErrorCode gp_camera_capture (HandleRef camera, CameraCaptureType type, out CameraFilePath path, HandleRef context);
		
		public CameraFilePath Capture (CameraCaptureType type, Context context)
		{
			CameraFilePath path;
			Error.CheckError (gp_camera_capture (this.Handle, type, out path, context.Handle));

			return path;
		}
		
		[DllImport ("libgphoto2.so")]
		internal unsafe static extern ErrorCode gp_camera_capture_preview (HandleRef camera, HandleRef file, HandleRef context);
		
		public void CapturePreview (CameraFile dest, Context context)
		{
			Error.CheckError (gp_camera_capture_preview (this.Handle, dest.Handle, context.Handle));
		}
#endregion

#region Operations on folders
		[DllImport ("libgphoto2.so")]
		internal static extern ErrorCode gp_camera_folder_list_files (HandleRef camera, [MarshalAs(UnmanagedType.LPTStr)] string folder, HandleRef list, HandleRef context);
		
		public CameraList ListFiles (string folder, Context context)
		{
			CameraList file_list = new CameraList ();
			Error.CheckError (gp_camera_folder_list_files(this.Handle, folder, file_list.Handle, context.Handle));

			return file_list;
		}
		
		[DllImport ("libgphoto2.so")]
		internal static extern ErrorCode gp_camera_folder_list_folders (HandleRef camera, [MarshalAs(UnmanagedType.LPTStr)] string folder, HandleRef list, HandleRef context);

		public CameraList ListFolders (string folder, Context context)
		{
			CameraList file_list = new CameraList();
			Error.CheckError (gp_camera_folder_list_folders (this.Handle, folder, file_list.Handle, context.Handle));

			return file_list;
		}
		
		[DllImport ("libgphoto2.so")]
		internal static extern ErrorCode gp_camera_folder_delete_all (HandleRef camera, [MarshalAs(UnmanagedType.LPTStr)] string folder, HandleRef context);
		
		public void DeleteAll (string folder, Context context)
		{
			Error.CheckError (gp_camera_folder_delete_all (this.Handle, folder, context.Handle));
		}
		
		[DllImport ("libgphoto2.so")]
		internal static extern ErrorCode gp_camera_folder_make_dir (HandleRef camera, [MarshalAs(UnmanagedType.LPTStr)] string folder, [MarshalAs(UnmanagedType.LPTStr)] string name, HandleRef context);
		
		public void MakeDirectory (string folder, string name, Context context)
		{
			Error.CheckError (gp_camera_folder_make_dir (this.Handle, folder, name, context.Handle));
		}
		
		[DllImport ("libgphoto2.so")]
		internal static extern ErrorCode gp_camera_folder_remove_dir (HandleRef camera, [MarshalAs(UnmanagedType.LPTStr)] string folder, [MarshalAs(UnmanagedType.LPTStr)] string name, HandleRef context);
		
		public void RemoveDirectory (string folder, string name, Context context)
		{
			Error.CheckError (gp_camera_folder_remove_dir(this.Handle, folder, name, context.Handle));
		}

		[DllImport ("libgphoto2.so")]
		internal unsafe static extern ErrorCode gp_camera_folder_put_file (HandleRef camera, [MarshalAs(UnmanagedType.LPTStr)] string folder, HandleRef file, HandleRef context);
		
		public void PutFile (string folder, CameraFile file, Context context)
		{
			Error.CheckError (gp_camera_folder_put_file(this.Handle, folder, file.Handle, context.Handle));
		}
#endregion		
			
#region Operations on files
		[DllImport ("libgphoto2.so")]
		internal static extern ErrorCode gp_camera_file_get (HandleRef camera, [MarshalAs(UnmanagedType.LPTStr)] string folder, [MarshalAs(UnmanagedType.LPTStr)] string file, CameraFileType type, HandleRef camera_file, HandleRef context);
		
		public void GetFile (string folder, string name, CameraFileType type, CameraFile camera_file, Context context)
		{
			Error.CheckError (gp_camera_file_get(this.Handle, folder, name, type, camera_file.Handle, context.Handle));	
		}
		
		[DllImport ("libgphoto2.so")]
		internal static extern ErrorCode gp_camera_file_delete (HandleRef camera, [MarshalAs(UnmanagedType.LPTStr)] string folder, [MarshalAs(UnmanagedType.LPTStr)] string file, HandleRef context);

		public void DeleteFile (string folder, string name, Context context)
		{
			Error.CheckError (gp_camera_file_delete(this.Handle, folder, name, context.Handle));
		}
		
		
		[DllImport ("libgphoto2.so")]
		internal static extern ErrorCode gp_camera_file_get_info (HandleRef camera, [MarshalAs(UnmanagedType.LPTStr)] string folder, [MarshalAs(UnmanagedType.LPTStr)] string file, out CameraFileInfo info, HandleRef context);
		
		public CameraFileInfo GetFileInfo (string folder, string name, Context context)
		{
			CameraFileInfo fileinfo;
			Error.CheckError (gp_camera_file_get_info(this.Handle, folder, name, out fileinfo, context.Handle));

			return fileinfo;
		}
		
		[DllImport ("libgphoto2.so")]
		internal static extern ErrorCode gp_camera_file_set_info (HandleRef camera, string folder, string file, CameraFileInfo info, HandleRef context);
		
		public void SetFileInfo (string folder, string name, CameraFileInfo fileinfo, Context context)
		{
			Error.CheckError (gp_camera_file_set_info(this.Handle, folder, name, fileinfo, context.Handle));
		}
#endregion			
	}
}
