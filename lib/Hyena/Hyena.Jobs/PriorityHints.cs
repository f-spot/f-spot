//
// PriorityHints.cs
//
// Author:
//   Gabriel Burt <gburt@novell.com>
//
// Copyright (C) 2009 Novell, Inc.
//
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using System;

namespace Hyena.Jobs
{
	[Flags]
	public enum PriorityHints
	{
		None = 0,
		DataLossIfStopped = 1,
		SpeedSensitive = 2,
		LongRunning = 4
	}
}
