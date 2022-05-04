//
// CanvasHost.cs
//
// Author:
//       Aaron Bockover <abockover@novell.com>
//
// Copyright 2009 Aaron Bockover
//
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
namespace Hyena.Gui.Canvas
{
	public class CanvasManager
	{
		ICanvasHost host;

		public CanvasManager (ICanvasHost host)
		{
			this.host = host;
		}

		public void QueueArrange (CanvasItem item)
		{
			item.Arrange ();
		}

		public void QueueMeasure (CanvasItem item)
		{
			item.Measure (item.ContentSize);
		}

		public void QueueRender (CanvasItem item, Rect rect)
		{
			if (host == null) {
				return;
			}

			host.QueueRender (item, rect);
		}

		public ICanvasHost Host {
			get { return host; }
		}
	}
}
