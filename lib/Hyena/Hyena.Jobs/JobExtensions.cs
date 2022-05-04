//
// JobExtensions.cs
//
// Author:
//   Gabriel Burt <gburt@novell.com>
//
// Copyright (C) 2009 Novell, Inc.
//
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Linq;

namespace Hyena.Jobs
{
	public static class JobExtensions
	{
		internal static IEnumerable<T> Without<T> (this IEnumerable<T> source, PriorityHints hints) where T : Job
		{
			return source.Where (j => !j.Has (hints));
		}

		internal static IEnumerable<T> With<T> (this IEnumerable<T> source, PriorityHints hints) where T : Job
		{
			return source.Where (j => j.Has (hints));
		}

		internal static IEnumerable<T> SharingResourceWith<T> (this IEnumerable<T> source, Job job) where T : Job
		{
			return source.Where (j => j.Resources.Intersect (job.Resources).Any ());
		}

		public static void ForEach<T> (this IEnumerable<T> source, Action<T> func)
		{
			foreach (T item in source)
				func (item);
		}

		public static bool Has<T> (this T job, PriorityHints hints) where T : Job
		{
			return (job.PriorityHints & hints) == hints;
		}

		// Useful..
		/*public static bool Include (this Enum source, Enum flags)
        {
            return ((int)source & (int)flags) == (int)flags;
        }*/
	}
}
