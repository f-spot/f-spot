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
