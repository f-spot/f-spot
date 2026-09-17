// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using System;
using System.IO;

using FSpot.Core;
using FSpot.Resources.Lang;
using FSpot.Settings;
using FSpot.Thumbnail;
using FSpot.Utils;

using Hyena;

namespace FSpot
{
	public class RotateException : ApplicationException
	{
		public bool ReadOnly;
		public string Path { get; private set; }

		public RotateException (string msg, string path) : this (msg, path, false) { }

		public RotateException (string msg, string path, bool ro) : base (msg)
		{
			Path = path;
			ReadOnly = ro;
		}
	}

	public enum RotateDirection
	{
		Clockwise,
		Counterclockwise,
	}

	public class RotateOperation
	{
		readonly IPhoto item;
		readonly RotateDirection direction;
		readonly IThumbnailService thumbnails;
		bool done;

		public RotateOperation (IPhoto item, RotateDirection direction, IThumbnailService thumbnails)
		{
			this.item = item;
			this.direction = direction;
			this.thumbnails = thumbnails;
		}

		public bool Step ()
		{
			if (done)
				return false;

			RotateOrientation (item.DefaultVersion.Uri, direction, thumbnails);
			done = true;
			return !done;
		}

		static void RotateOrientation (SafeUri uri, RotateDirection direction, IThumbnailService thumbnails)
		{
			try {
				// A missing parent throws before any metadata parse. Keep it inside
				// this try so the caller gets a RotateException for this uri.
				var attrs = File.GetAttributes (uri.AbsolutePath);
				if (attrs.HasFlag (FileAttributes.ReadOnly))
					throw new RotateException (Strings.UnableToRotateReadonlyFile, uri, true);

				using var metadata = MetadataUtils.Parse (uri);

				metadata.EnsureAvailableTags ();
				var tag = metadata.ImageTag;
				tag.Orientation = direction == RotateDirection.Clockwise
					? ImageOrientations.Rotate90 (tag.Orientation)
					: ImageOrientations.Rotate270 (tag.Orientation);

				bool always_sidecar = Preferences.Get<bool> (Preferences.MetadataAlwaysUseSidecar);
				metadata.SaveSafely (uri, always_sidecar);
				thumbnails?.DeleteThumbnails (uri);
			} catch (RotateException) {
				throw;
			} catch (Exception e) {
				Logger.Log.Debug (e, "");
				throw new RotateException (Strings.UnableToRotateThisTypeOfPhoto, uri);
			}
		}
	}

	public class RotateMultiple
	{
		readonly RotateDirection direction;
		readonly IThumbnailService thumbnails;
		RotateOperation op;

		public int Index { get; private set; }

		public IPhoto[] Items { get; private set; }

		public IPhoto FailedItem { get; private set; }

		public RotateMultiple (IPhoto[] items, RotateDirection direction, IThumbnailService thumbnails)
		{
			this.direction = direction;
			this.thumbnails = thumbnails;
			Items = items;
			Index = 0;
		}

		public bool Step ()
		{
			if (Index >= Items.Length)
				return false;

			if (op == null)
				op = new RotateOperation (Items[Index], direction, thumbnails);

			try {
				if (op.Step ())
					return true;
			} catch {
				// Leave the failed photo behind. Retrying it forever is how the
				// GTK rotate command hung on a read-only file. Remember it before
				// Index moves, or the error dialog reads the next photo.
				FailedItem = Items[Index];
				op = null;
				Index++;
				throw;
			}

			Index++;
			op = null;
			return Index < Items.Length;
		}

		public string FailedPath ()
		{
			IPhoto photo = FailedItem;
			if (photo == null && Index >= 0 && Index < Items.Length)
				photo = Items[Index];
			if (photo == null || photo.DefaultVersion == null || photo.DefaultVersion.Uri == null)
				return string.Empty;

			string local = photo.DefaultVersion.Uri.LocalPath;
			return string.IsNullOrEmpty (local) ? photo.DefaultVersion.Uri.ToString () : local;
		}
	}
}
