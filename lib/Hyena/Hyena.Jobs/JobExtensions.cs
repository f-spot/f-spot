//
// JobExtensions.cs
//
// Author:
//   Gabriel Burt <gburt@novell.com>
//
// Copyright (C) 2009 Novell, Inc.
//
// Permission is hereby granted, free of charge, to any person obtaining
// a copy of this software and associated documentation files (the
// "Software"), to deal in the Software without restriction, including
// without limitation the rights to use, copy, modify, merge, publish,
// distribute, sublicense, and/or sell copies of the Software, and to
// permit persons to whom the Software is furnished to do so, subject to
// the following conditions:
//
// The above copyright notice and this permission notice shall be
// included in all copies or substantial portions of the Software.
//
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND,
// EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF
// MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND
// NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE
// LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION
// OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION
// WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
//

using System;
using System.Linq;
using System.Collections.Generic;

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
