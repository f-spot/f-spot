// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using System;

using FSpot.Core;
using FSpot.Database;
using FSpot.Imaging;

namespace FSpot.Editors
{
	public static class PhotoEditorApply
	{
		public static void Apply (PhotoEditorDescriptor editor, FSpot.Photo[] photos, PhotoEditorContext context,
			IImageFileFactory factory, PhotoStore store, Action<int> step = null)
		{
			if (editor == null)
				throw new ArgumentNullException (nameof (editor));
			if (!PhotoEditorOperations.CanProcess (editor))
				throw new NotSupportedException (editor.Id);
			if (photos == null)
				throw new ArgumentNullException (nameof (photos));
			if (factory == null)
				throw new ArgumentNullException (nameof (factory));

			int done = 0;
			foreach (var photo in photos) {
				if (photo == null)
					continue;

				using (var image = factory.Create (photo.DefaultVersion.Uri)) {
					var input = image.Load ();
					var edited = PhotoEditorOperations.Process (editor, input, image.GetProfile (), context ?? new PhotoEditorContext ());
					photo.SaveVersion (edited, photo.DefaultVersion.IsProtected);
					photo.Changes.DataChanged = true;
					store?.Commit (photo);
				}

				done++;
				step?.Invoke (done);
			}
		}
	}
}
