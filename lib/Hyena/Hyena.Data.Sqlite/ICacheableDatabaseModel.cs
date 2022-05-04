//
// ICacheableDatabaseModel.cs
//
// Author:
//   Gabriel Burt <gburt@novell.com>
//
// Copyright (C) 2007 Novell, Inc.
//
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

namespace Hyena.Data.Sqlite
{
	public interface ICacheableDatabaseModel : ICacheableModel
	{
		string ReloadFragment { get; }
		string SelectAggregates { get; }
		string JoinTable { get; }
		string JoinFragment { get; }
		string JoinPrimaryKey { get; }
		string JoinColumn { get; }
		bool CachesJoinTableEntries { get; }
		bool CachesValues { get; }
	}
}
