//
// ISelectable.cs
//
// Author:
//   Aaron Bockover <abockover@novell.com>
//
// Copyright (C) 2007 Novell, Inc.
//
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using Hyena.Collections;

namespace Hyena.Data
{
	public interface ISelectable
	{
		Selection Selection { get; }
	}
}
