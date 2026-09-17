// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using System;

using FSpot.Core;

namespace FSpot.Editors
{
	public sealed class PhotoEditorDescriptor
	{
		readonly Func<string> label;

		public PhotoEditorDescriptor (string id, Func<string> label, string iconName,
			bool needsSelection = false, bool canHandleMultiple = false, bool hasSettings = false,
			bool hasCoreOperation = true)
		{
			Id = id;
			this.label = label;
			IconName = iconName;
			NeedsSelection = needsSelection;
			CanHandleMultiple = canHandleMultiple;
			HasSettings = hasSettings;
			HasCoreOperation = hasCoreOperation;
		}

		public string Id { get; }
		public string Label => label ();
		public string IconName { get; }
		public bool NeedsSelection { get; }
		public bool CanHandleMultiple { get; }
		public bool HasSettings { get; }
		public bool HasCoreOperation { get; }

		public bool VisibleIn (LibraryViewMode mode)
			=> mode == LibraryViewMode.PhotoView || CanHandleMultiple;
	}
}
