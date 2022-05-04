//
// Roll.cs
//
// Author:
//   Stephane Delcroix <sdelcroix@src.gnome.org>
//   Evan Briones <erbriones@gmail.com>
//   Ruben Vermeersch <ruben@savanne.be>
//
// Copyright (C) 2008-2010 Novell, Inc.
// Copyright (C) 2008 Stephane Delcroix
// Copyright (C) 2010 Evan Briones
// Copyright (C) 2010 Ruben Vermeersch
//
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using System;

using Hyena;

namespace FSpot.Core
{
	public class Roll : DbItem
	{
		// The time is always in UTC.
		public DateTime Time { get; private set; }

		public Roll (uint id, long unixTime) : base (id)
		{
			Time = DateTimeUtil.ToDateTime (unixTime);
		}
	}
}
