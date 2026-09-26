// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

using FSpot.FileSystem;
using FSpot.Resources.Lang;
using FSpot.Utils;

using Hyena;

namespace FSpot
{
	// A source offered by the import source picker: a mounted camera, card or
	// disk, matching what FSpot.Gtk's ImportDialogController.ScanSources /
	// FSpot.Import.ImportSource show in the "Import from:" combo.
	// A plain struct rather than a record: FSpot also builds for net472, which has no IsExternalInit.
	public readonly struct ImportSourceInfo
	{
		public ImportSourceInfo (string name, SafeUri root, string iconName)
		{
			Name = name;
			Root = root;
			IconName = iconName;
		}

		public string Name { get; }
		public SafeUri Root { get; }
		public string IconName { get; }

		public const string CameraIcon = "media-flash";
		public const string IPodIcon = "multimedia-player";
		public const string DriveIcon = "drive-harddisk";
	}

	public static class DeviceImport
	{
		public static string EmptyMessage => Strings.NoCamerasDetected;

		public static IReadOnlyList<ImportSourceInfo> Sources (IFileSystem fileSystem)
			=> ReadyDrives ().Select (drive => Describe (drive, fileSystem)).ToList ();

		public static IReadOnlyList<string> Cameras (IFileSystem fileSystem)
			=> Sources (fileSystem).Select (source => source.Name).ToList ();

		internal static IEnumerable<DriveInfo> ReadyDrives ()
		{
			DriveInfo[] drives;
			try {
				drives = DriveInfo.GetDrives ();
			} catch (IOException) {
				yield break;
			} catch (UnauthorizedAccessException) {
				yield break;
			}

			foreach (var drive in drives) {
				bool ready;
				try {
					ready = drive.IsReady;
				} catch (IOException) {
					ready = false;
				} catch (UnauthorizedAccessException) {
					ready = false;
				}

				if (ready)
					yield return drive;
			}
		}

		internal static ImportSourceInfo Describe (DriveInfo drive, IFileSystem fileSystem)
		{
			string label = string.IsNullOrEmpty (drive.VolumeLabel) ? "Local Disk" : drive.VolumeLabel;
			var root = new SafeUri (drive.RootDirectory.FullName);
			return Describe (root, $"{label} ({drive.Name})", fileSystem);
		}

		internal static ImportSourceInfo Describe (SafeUri root, string name, IFileSystem fileSystem)
			=> new (name, root, IconFor (root, fileSystem));

		// Mirrors FSpot.Import.ImportSource.IsCamera / IsIPodPhoto in FSpot.Gtk,
		// but checks the marker folders with IDirectory rather than IFile so a
		// DCIM or iPod_Control directory is actually detected.
		internal static string IconFor (SafeUri root, IFileSystem fileSystem)
		{
			if (fileSystem.Directory.Exists (root.Append ("Photos")) && fileSystem.Directory.Exists (root.Append ("iPod_Control")))
				return ImportSourceInfo.IPodIcon;
			if (fileSystem.Directory.Exists (root.Append ("DCIM")))
				return ImportSourceInfo.CameraIcon;
			return ImportSourceInfo.DriveIcon;
		}
	}
}
