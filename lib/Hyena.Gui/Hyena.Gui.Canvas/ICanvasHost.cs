//
// ICanvasHost.cs
//
// Author:
//   Gabriel Burt <gburt@novell.com>
//
// Copyright 2010 Novell, Inc.
//
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
namespace Hyena.Gui.Canvas
{
	public interface ICanvasHost
	{
		void QueueRender (CanvasItem item, Rect rect);
		Pango.Layout PangoLayout { get; }
		Pango.FontDescription FontDescription { get; }
	}
}
