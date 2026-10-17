// Licensed under the MIT License. See LICENSE file in the project root for full license information.

namespace FSpot.Loaders
{
	// A thumbnail handed to the view belongs to the view. The loader disposes
	// one only when the view never claimed it, and never on the decode thread.
	public sealed class ThumbnailHandoff
	{
		bool held;
		bool claimed;

		public void Hold ()
		{
			held = true;
			claimed = false;
		}

		public void Claim ()
		{
			if (held)
				claimed = true;
		}

		// True when the loader may dispose the thumbnail. False when the view
		// still holds it and will dispose it once.
		public bool Release ()
		{
			if (!held)
				return false;
			held = false;
			bool dispose = !claimed;
			claimed = false;
			return dispose;
		}
	}
}
