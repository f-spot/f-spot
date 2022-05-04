//
// NotifyEventArgs.cs
//
// Author:
//   Stephane Delcroix <sdelcroix@novell.com>
//   Stephen Shaw <sshaw@decriptor.com>
//
// Copyright (C) 2008 Novell, Inc.
// Copyright (C) 2008 Stephane Delcroix
// Copyright (C) 2019 Stephen Shaw
//
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using System;

namespace FSpot
{
	public class NotifyEventArgs : EventArgs
	{
		public string Key { get; }
		public object Value { get; }

		public NotifyEventArgs (string key, object val)
		{
			Key = key;
			Value = val;
		}
	}
}
