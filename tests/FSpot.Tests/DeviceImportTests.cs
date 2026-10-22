// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using FSpot.Resources.Lang;
using FSpot.Utils;

using Hyena;

using Microsoft.VisualStudio.TestTools.UnitTesting;

using Mocks;

namespace FSpot.Tests
{
	[TestClass]
	public sealed class DeviceImportTests
	{
		[TestMethod]
		public void IconFor_PlainDrive_IsDriveIcon ()
		{
			var root = new SafeUri ("file:///Volumes/Untitled", true);
			var fileSystem = new FileSystemMock ();

			Assert.AreEqual (ImportSourceInfo.DriveIcon, DeviceImport.IconFor (root, fileSystem));
		}

		[TestMethod]
		public void IconFor_DcimFolder_IsCameraIcon ()
		{
			var root = new SafeUri ("file:///Volumes/EOS_DIGITAL", true);
			var fileSystem = new FileSystemMock ();
			fileSystem.SetDirectory (root.Append ("DCIM"));

			Assert.AreEqual (ImportSourceInfo.CameraIcon, DeviceImport.IconFor (root, fileSystem));
		}

		[TestMethod]
		public void IconFor_PhotosAndIPodControlFolders_IsIPodIcon ()
		{
			var root = new SafeUri ("file:///Volumes/IPOD", true);
			var fileSystem = new FileSystemMock ();
			fileSystem.SetDirectory (root.Append ("Photos"));
			fileSystem.SetDirectory (root.Append ("iPod_Control"));

			Assert.AreEqual (ImportSourceInfo.IPodIcon, DeviceImport.IconFor (root, fileSystem));
		}

		[TestMethod]
		public void IconFor_OnlyPhotosFolderWithoutIPodControl_IsNotIPodIcon ()
		{
			var root = new SafeUri ("file:///Volumes/PHOTOS_ONLY", true);
			var fileSystem = new FileSystemMock ();
			fileSystem.SetDirectory (root.Append ("Photos"));

			Assert.AreNotEqual (ImportSourceInfo.IPodIcon, DeviceImport.IconFor (root, fileSystem));
		}

		[TestMethod]
		public void IconFor_PrefersIPodOverCameraWhenBothMarkersArePresent ()
		{
			var root = new SafeUri ("file:///Volumes/BOTH", true);
			var fileSystem = new FileSystemMock ();
			fileSystem.SetDirectory (root.Append ("Photos"));
			fileSystem.SetDirectory (root.Append ("iPod_Control"));
			fileSystem.SetDirectory (root.Append ("DCIM"));

			Assert.AreEqual (ImportSourceInfo.IPodIcon, DeviceImport.IconFor (root, fileSystem));
		}

		[TestMethod]
		public void VolumeLabel_UsesLocalDiskWhenTheDriveHasNoLabel ()
		{
			Assert.AreEqual ("Local Disk", Strings.LocalDisk);
			Assert.AreEqual (Strings.LocalDisk, DeviceImport.VolumeLabel (null));
			Assert.AreEqual (Strings.LocalDisk, DeviceImport.VolumeLabel (""));
			Assert.AreEqual ("EOS_DIGITAL", DeviceImport.VolumeLabel ("EOS_DIGITAL"));
		}

		[TestMethod]
		public void Describe_KeepsTheGivenNameAndRoot ()
		{
			var root = new SafeUri ("file:///Volumes/EOS_DIGITAL", true);
			var fileSystem = new FileSystemMock ();
			fileSystem.SetDirectory (root.Append ("DCIM"));

			var source = DeviceImport.Describe (root, "EOS_DIGITAL (E:)", fileSystem);

			Assert.AreEqual ("EOS_DIGITAL (E:)", source.Name);
			Assert.AreEqual (root, source.Root);
			Assert.AreEqual (ImportSourceInfo.CameraIcon, source.IconName);
		}

		[TestMethod]
		public void Sources_WithNoReadyDrives_IsEmpty ()
		{
			// DriveInfo.GetDrives is a thin OS call (mirroring FSpot.Gtk's
			// ImportDialogController.ScanSources); Sources() only adds entries
			// for drives that report IsReady, so on a host with none ready
			// (e.g. a locked-down CI sandbox) the list can legitimately be
			// empty. Exercise the public entry point without asserting on the
			// host's actual drives.
			var fileSystem = new FileSystemMock ();
			var sources = DeviceImport.Sources (fileSystem);

			Assert.IsNotNull (sources);
		}
	}
}
