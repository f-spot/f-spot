//
// ImageView_Panning.cs
//
// Author:
//   Ruben Vermeersch <ruben@savanne.be>
//
// Copyright (C) 2010 Novell, Inc.
// Copyright (C) 2010 Ruben Vermeersch
//
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using System.Diagnostics;

using Gdk;

namespace FSpot.Widgets
{
	public partial class ImageView
	{
		/// <summary>
		/// Whether or not the user is currently performing a pan motion (dragging with the middle mouse button).
		/// </summary>
		///
		public bool InPanMotion { get; private set; }

		Point panAnchor = new Point (0, 0);

		bool OnPanButtonPressEvent (EventButton evnt)
		{
			if (evnt.Button != 2) {
				// Restrict to middle mouse button.
				return false;
			}

			Debug.Assert (!InPanMotion);
			InPanMotion = true;

			// Track starting point of panning movement.
			panAnchor.X = (int)evnt.X;
			panAnchor.Y = (int)evnt.Y;

			// Set to crosshair pointer
			GdkWindow.Cursor = new Cursor (CursorType.Fleur);
			return true;
		}

		bool OnPanMotionNotifyEvent (EventMotion evnt)
		{
			if (!InPanMotion) {
				return false;
			}

			// Calculate the direction of the panning, scroll accordingly.
			int panX = panAnchor.X - (int)evnt.X;
			int panY = panAnchor.Y - (int)evnt.Y;
			ScrollBy (panX, panY);

			// Reset starting point.
			panAnchor.X = (int)evnt.X;
			panAnchor.Y = (int)evnt.Y;
			return true;
		}

		bool OnPanButtonReleaseEvent (EventButton evnt)
		{
			if (evnt.Button != 2) {
				// Restrict to middle mouse button.
				return false;
			}

			Debug.Assert (InPanMotion);
			InPanMotion = false;

			// Reset cursor
			GdkWindow.Cursor = null;
			return true;
		}
	}
}
