//
// XdgDirectoryService.cs
//
// Author:
//   Daniel Köb <daniel.koeb@peony.at>
//
// Copyright (C) 2016 Daniel Köb
//
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using System.IO;

using FSpot.FileSystem;

namespace FSpot.Thumbnail
{
	/// <summary>
	/// Implements some XDG directory specification
	/// http://standards.freedesktop.org/basedir-spec/basedir-spec-latest.html
	/// </summary>
	class XdgDirectoryService : IXdgDirectoryService
	{
		readonly IFileSystem fileSystem;
		readonly IEnvironment environment;
		readonly object cacheLock = new object ();

		string userCacheDir;

		public XdgDirectoryService (IFileSystem fileSystem, IEnvironment environment)
		{
			this.fileSystem = fileSystem;
			this.environment = environment;
		}

		/// <summary>
		/// Gets the XDG user cache dir.
		///
		/// There is a single base directory relative to which user-specific non-essential (cached) data should be
		/// written. This directory is defined by the environment variable $XDG_CACHE_HOME.
		///
		/// Re-implements glib's g_get_user_cache_dir
		/// </summary>
		/// <returns>The user cache dir.</returns>
		public string GetUserCacheDir ()
		{
			lock (cacheLock) {
				if (string.IsNullOrEmpty (userCacheDir)) {
					userCacheDir = environment.GetEnvironmentVariable ("XDG_CACHE_HOME");

					if (string.IsNullOrEmpty (userCacheDir)) {
						string homeDir = environment.GetEnvironmentVariable ("HOME");

						userCacheDir = string.IsNullOrEmpty (homeDir)
							? Path.Combine (fileSystem.Path.GetTempPath (), environment.UserName, ".cache")
							: Path.Combine (homeDir, ".cache");
					}
				}
			}
			return userCacheDir;
		}

		/// <summary>
		/// Gets the thumbnails dir based on the user cache dir and the thumbnail size.
		///
		/// http://specifications.freedesktop.org/thumbnail-spec/thumbnail-spec-latest.html
		/// </summary>
		/// <returns>The thumbnails dir.</returns>
		/// <param name="size">The thumbnail size to get the dir for.</param>
		public string GetThumbnailsDir (ThumbnailSize size)
		{
			return Path.Combine (GetUserCacheDir (), "thumbnails", size == ThumbnailSize.Normal ? "normal" : "large");
		}
	}
}
