//
// IInteractiveCell.cs
//
// Author:
//   Aaron Bockover <abockover@novell.com>
//
// Copyright (C) 2008 Novell, Inc.
//
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

namespace Hyena.Data.Gui
{
	public interface IInteractiveCell
	{
		bool ButtonEvent (Hyena.Gui.Canvas.Point cursor, bool pressed, uint button);
		bool CursorMotionEvent (Hyena.Gui.Canvas.Point cursor);
		bool CursorLeaveEvent ();
	}
}
