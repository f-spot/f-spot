//
// IDataBinder.cs
//
// Author:
//   Gabriel Burt <gburt@novell.com>
//
// Copyright (C) 2010 Novell, Inc.
//
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

namespace Hyena.Data
{
	public interface IDataBinder
	{
		void Bind (object o);
		object BoundObject { get; set; }
	}
}
