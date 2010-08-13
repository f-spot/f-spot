using System;
using System.IO;

namespace FSpot.Core
{
    public static class IPhotoExtensions
    {
        public static int CompareDate (this IPhoto photo1, IPhoto photo2)
        {
            return DateTime.Compare (photo1.Time, photo2.Time);
        }

        public static int CompareName (this IPhoto photo1, IPhoto photo2)
        {
            return string.Compare (photo1.Name, photo2.Name);
        }

        public static int Compare (this IPhoto photo1, IPhoto photo2)
        {
            int result = photo1.CompareDate (photo2);
            
            if (result == 0)
                result = CompareDefaultVersionUri (photo1, photo2);
            
            if (result == 0)
                result = photo1.CompareName (photo2);
            
            return result;
        }

        public static int CompareDefaultVersionUri (this IPhoto photo1, IPhoto photo2)
        {
            var photo1_uri = Path.Combine (photo1.DefaultVersion.BaseUri, photo1.DefaultVersion.Filename);
            var photo2_uri = Path.Combine (photo2.DefaultVersion.BaseUri, photo2.DefaultVersion.Filename);
            return string.Compare (photo1_uri, photo2_uri);
        }
    }
}
