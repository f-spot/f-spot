//
// ImportPreferences.cs
//
// Author:
//   Stephen Shaw <sshaw@decriptor.com>
//
// Copyright (C) 2020 Stephen Shaw
//
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using FSpot.Settings;

namespace FSpot.Import
{
	public class ImportPreferences
	{
		readonly bool persistPreferences;
		bool copyFiles = true;
		bool removeOriginals;
		bool recurseSubdirectories = true;
		bool duplicateDetect = true;
		bool mergeRawAndJpeg = true;

		public ImportPreferences (bool persistPreferences)
		{
			// This flag determines whether or not the chosen options will be
			// saved. You don't want to overwrite user preferences when running
			// headless.
			this.persistPreferences = persistPreferences;
			LoadPreferences ();
		}
		public bool CopyFiles {
			get { return copyFiles; }
			set { copyFiles = value; SavePreferences (); }
		}

		public bool RemoveOriginals {
			get { return removeOriginals; }
			set { removeOriginals = value; SavePreferences (); }
		}

		public bool RecurseSubdirectories {
			get { return recurseSubdirectories; }
			set {
				if (recurseSubdirectories == value)
					return;
				recurseSubdirectories = value;
				SavePreferences ();
				//StartScan ();
			}
		}

		public bool DuplicateDetect {
			get { return duplicateDetect; }
			set { duplicateDetect = value; SavePreferences (); }
		}

		public bool MergeRawAndJpeg {
			get { return mergeRawAndJpeg; }
			set {
				if (mergeRawAndJpeg == value)
					return;
				mergeRawAndJpeg = value;
				SavePreferences ();
				// FIXME?
				//StartScan ();
			}
		}

		void LoadPreferences ()
		{
			if (!persistPreferences)
				return;

			copyFiles = Preferences.Get<bool> (Preferences.ImportCopyFiles);
			recurseSubdirectories = Preferences.Get<bool> (Preferences.ImportIncludeSubfolders);
			duplicateDetect = Preferences.Get<bool> (Preferences.ImportCheckDuplicates);
			removeOriginals = Preferences.Get<bool> (Preferences.ImportRemoveOriginals);
			mergeRawAndJpeg = Preferences.Get<bool> (Preferences.ImportMergeRawAndJpeg);
		}

		void SavePreferences ()
		{
			if (!persistPreferences)
				return;

			Preferences.Set (Preferences.ImportCopyFiles, copyFiles);
			Preferences.Set (Preferences.ImportIncludeSubfolders, recurseSubdirectories);
			Preferences.Set (Preferences.ImportCheckDuplicates, duplicateDetect);
			Preferences.Set (Preferences.ImportRemoveOriginals, removeOriginals);
			Preferences.Set (Preferences.ImportMergeRawAndJpeg, mergeRawAndJpeg);
		}
	}
}
