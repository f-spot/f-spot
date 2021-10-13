//
// ILimitable.cs
//
// Author:
//   Larry Ewing <lewing@novell.com>
//   Thomas Van Machelen <thomas.vanmachelen@gmail.com>
//
// Copyright (C) 2004-2007 Novell, Inc.
// Copyright (C) 2004 Larry Ewing
// Copyright (C) 2007 Thomas Van Machelen
//
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

namespace FSpot
{
	public interface ILimitable
	{
		void SetLimits (int min, int max);
	}
}
