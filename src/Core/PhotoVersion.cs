/*
 * PhotoStore.cs
 *
 * Author(s):
 *	Ettore Perazzoli <ettore@perazzoli.org>
 *	Larry Ewing <lewing@gnome.org>
 *	Stephane Delcroix <stephane@delcroix.org>
 *	Thomas Van Machelen <thomas.vanmachelen@gmail.com>
 * 
 * This is free software. See COPYING for details.
 */

using Hyena;

namespace FSpot
{
    public class PhotoVersion : IBrowsableItemVersion
    {
        public string Name { get; set; }
        public IBrowsableItem Photo { get; private set; }
        public SafeUri BaseUri { get; set; }
        public string Filename { get; set; }
        public SafeUri Uri {
            get { return BaseUri.Append (Filename); }
            set {
                BaseUri = value.GetBaseUri ();
                Filename = value.GetFilename ();
            }
        }
        public string ImportMD5 { get; set; }
        public uint VersionId { get; private set; }
        public bool IsProtected { get; private set; }

        public PhotoVersion (IBrowsableItem photo, uint version_id, SafeUri base_uri, string filename, string md5_sum, string name, bool is_protected)
        {
            Photo = photo;
            VersionId = version_id;
            BaseUri = base_uri;
            Filename = filename;
            ImportMD5 = md5_sum;
            Name = name;
            IsProtected = is_protected;
        }
    }
}
