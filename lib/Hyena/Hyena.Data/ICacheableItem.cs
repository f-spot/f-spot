//
// ICacheableItem.cs
//
// Author:
//   Gabriel Burt <gburt@novell.com>
//
// Copyright (C) 2008 Novell, Inc.
//
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

namespace Hyena.Data
{
	public interface ICacheableItem
	{
		object CacheEntryId { get; set; }
		long CacheModelId { get; set; }
	}
}
