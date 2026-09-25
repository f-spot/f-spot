// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;

namespace FSpot
{
	public static class PhotoVersionWorkflow
	{
		public static uint Create (Photo photo, string name, Action<Photo> commit)
		{
			if (photo == null)
				throw new ArgumentNullException (nameof (photo));
			uint id = photo.CreateVersion (name, photo.DefaultVersionId, true);
			photo.DefaultVersionId = id;
			commit?.Invoke (photo);
			return id;
		}

		public static void Rename (Photo photo, string newName, Action<Photo> commit)
		{
			if (photo == null)
				throw new ArgumentNullException (nameof (photo));
			photo.RenameVersion (photo.DefaultVersionId, newName);
			commit?.Invoke (photo);
		}

		public static void Delete (Photo photo, Action<Photo> commit)
		{
			if (photo == null)
				throw new ArgumentNullException (nameof (photo));
			photo.DeleteVersion (photo.DefaultVersionId);
			commit?.Invoke (photo);
		}

		public static Photo Detach (Photo photo, Func<Photo, Photo> createFrom, Action<IReadOnlyList<Photo>> commit)
		{
			if (photo == null)
				throw new ArgumentNullException (nameof (photo));
			if (createFrom == null)
				throw new ArgumentNullException (nameof (createFrom));
			var created = createFrom (photo);
			created.CopyAttributesFrom (photo);
			photo.DeleteVersion (photo.DefaultVersionId, false, true);
			commit?.Invoke (new[] { created, photo });
			return created;
		}

		/// <summary>
		/// Folds every photo in <paramref name="photos"/> into <paramref name="newParent"/> as versions,
		/// the way the GTK client merges a photo dropped onto another one: the parent keeps the highest
		/// rating, its own description (or the first non-empty one), and the union of the tags. The
		/// merged photos lose their catalog entries but keep their files on disk.
		/// </summary>
		public static void Reparent (Photo newParent, IReadOnlyList<Photo> photos, Action<Photo> commit, Action<Photo[]> remove)
		{
			if (newParent == null)
				throw new ArgumentNullException (nameof (newParent));
			if (photos == null)
				throw new ArgumentNullException (nameof (photos));

			uint highestRating = newParent.Rating;
			string description = newParent.Description;
			foreach (var photo in photos) {
				if (photo == null || ReferenceEquals (photo, newParent))
					continue;
				highestRating = Math.Max (photo.Rating, highestRating);
				if (string.IsNullOrEmpty (description))
					description = photo.Description;
				newParent.AddTag (photo.Tags);

				foreach (uint versionId in photo.VersionIds) {
					newParent.DefaultVersionId = newParent.CreateReparentedVersion (photo.GetVersion (versionId) as PhotoVersion);
					commit?.Invoke (newParent);
				}
				var versionIds = photo.VersionIds;
				Array.Reverse (versionIds);
				foreach (uint versionId in versionIds)
					photo.DeleteVersion (versionId, true, true);
				remove?.Invoke (new[] { photo });
			}
			newParent.Rating = highestRating;
			newParent.Description = description;
			commit?.Invoke (newParent);
		}
	}
}
