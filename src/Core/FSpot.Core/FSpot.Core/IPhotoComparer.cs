//
// IPhotoComparer.cs
//
// Author:
//   Ruben Vermeersch <ruben@savanne.be>
//
// Copyright (C) 2010 Novell, Inc.
// Copyright (C) 2010 Ruben Vermeersch
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
// THE SOFTWARE IS PROVIDED AS IS, WITHOUT WARRANTY OF ANY KIND,
// EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF
// MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND
// NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE
// LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION
// OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION
// WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
//

using System;
using System.Collections;
using System.Collections.Generic;

namespace FSpot.Core
{
    public static class IPhotoComparer
    {
        public class CompareDateName : IComparer<IPhoto>
        {
            public int Compare (IPhoto p1, IPhoto p2)
            {
                int result = p1.CompareDate (p2);
                
                if (result == 0)
                    result = p1.CompareName (p2);
                
                return result;
            }
        }

        public class RandomSort : IComparer
        {
            Random random = new Random ();

            public int Compare (object obj1, object obj2)
            {
                return random.Next (-5, 5);
            }
        }

        public class CompareDirectory : IComparer<IPhoto>
        {
            public int Compare (IPhoto p1, IPhoto p2)
            {
                int result = p1.CompareDefaultVersionUri (p2);
                
                if (result == 0)
                    result = p1.CompareName (p2);
                
                return result;
            }
        }
    }
}
