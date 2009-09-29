/*
 * CameraFilesystem.cs
 *
 * Author(s):
 *	Ewen Cheslack-Postava <echeslack@gmail.com>
 *	Larry Ewing <lewing@novell.com>
 *
 * This is free software. See COPYING for details.
 */
using System;
using System.Runtime.InteropServices;

namespace GPhoto2
{
	public enum CameraFilePermissions
	{
		None = 0,
		Read = 1 << 0,
		Delete = 1 << 1,
		All = 0xFF
	}

	public enum CameraFileStatus
	{
		NotDownloaded,
		Downloaded
	}
	
	public enum CameraFileInfoFields
	{
		None		= 0,
		Type		= 1 << 0,
		Name		= 1 << 1,
		Size		= 1 << 2,
		Width		= 1 << 3,
		Height		= 1 << 4,
		Permissions	= 1 << 5,
		Status		= 1 << 6,
		MTime		= 1 << 7,
		All		= 0xFF
	}
	
	[StructLayout(LayoutKind.Sequential)]
	public unsafe struct CameraFileInfoAudio
	{
		public CameraFileInfoFields fields;
		public CameraFileStatus status;
		public ulong size;
		[MarshalAs(UnmanagedType.ByValTStr, SizeConst=64)] public char[] type;
	}
	
	[StructLayout(LayoutKind.Sequential)]
	public unsafe struct CameraFileInfoPreview
	{
		public CameraFileInfoFields fields;
		public CameraFileStatus status;
		public ulong size;
		[MarshalAs(UnmanagedType.ByValTStr, SizeConst=64)] public char[] type;
		
		public uint width, height;
	}
	
	[StructLayout(LayoutKind.Sequential)]
	public unsafe struct CameraFileInfoFile
	{
		public CameraFileInfoFields fields;
		public CameraFileStatus status;
		public ulong size;
		[MarshalAs(UnmanagedType.ByValTStr, SizeConst=64)] public char[] type;
		
		public uint width, height;
		[MarshalAs(UnmanagedType.ByValTStr, SizeConst=64)] public char[] name;
		public CameraFilePermissions permissions;
		public long time;
	}
	
	[StructLayout(LayoutKind.Sequential)]
	public unsafe struct CameraFileInfo
	{
		public CameraFileInfoPreview preview;
		public CameraFileInfoFile file;
		public CameraFileInfoAudio audio;
	}
}
