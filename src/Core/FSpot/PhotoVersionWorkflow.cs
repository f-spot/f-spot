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
	}
}
