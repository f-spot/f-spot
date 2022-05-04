//
// ITooltipCell.cs
//
// Author:
//   Gabriel Burt <gburt@novell.com>
//
// Copyright (C) 2009 Novell, Inc.
//
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

namespace Hyena.Data.Gui
{
	public interface ITooltipCell
	{
		string GetTooltipMarkup (CellContext cellContext, double columnWidth);
	}
}
