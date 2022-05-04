//
// ISizeRequestCell.cs
//
// Author:
//   Aaron Bockover <abockover@novell.com>
//
// Copyright (C) 2008 Novell, Inc.
//
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

namespace Hyena.Data.Gui
{
	public interface ISizeRequestCell
	{
		bool RestrictSize { get; set; }
		void GetWidthRange (Pango.Layout layout, out int min_width, out int max_width);
	}
}
