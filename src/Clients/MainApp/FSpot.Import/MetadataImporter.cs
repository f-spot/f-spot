using System;
using Mono.Unix;
using System.Collections.Generic;
using FSpot.Core;
using FSpot.Utils;

namespace FSpot.Import {
    internal class MetadataImporter {
        private TagStore tag_store;
        private Stack<Tag> tags_created;

        static private string LastImportIcon = "gtk-new";

        private class TagInfo {
            // This class contains the Root tag name, and its Icon name (if any)
            string tag_name;
            string icon_name;

            public string TagName {
                get { return tag_name; }
            }

            public string IconName {
                get { return icon_name; }
            }

            public bool HasIcon {
                get { return icon_name != null; }
            }

            public TagInfo (string t_name, string i_name)
            {
                tag_name = t_name;
                icon_name = i_name;
            }

            public TagInfo (string t_name)
            {
                tag_name = t_name;
                icon_name = null;
            }
        } // TagInfo

        TagInfo li_root_tag; // This is the Last Import root tag

        public MetadataImporter ()
        {
            this.tag_store = App.Instance.Database.Tags;
            tags_created = new Stack<Tag> ();

            li_root_tag = new TagInfo (Catalog.GetString ("Imported Tags"), LastImportIcon);
        }

        private Tag EnsureTag (TagInfo info, Category parent)
        {
            Tag tag = tag_store.GetTagByName (info.TagName);

            if (tag != null)
                return tag;

            tag = tag_store.CreateCategory (parent,
                    info.TagName,
                    false);

            if (info.HasIcon) {
                tag.ThemeIconName = info.IconName;
                tag_store.Commit(tag);
            }

            tags_created.Push (tag);
            return tag;
        }

        private void AddTagToPhoto (Photo photo, string new_tag_name)
        {
            if (new_tag_name == null || new_tag_name.Length == 0)
                return;

            Tag parent = EnsureTag (li_root_tag, tag_store.RootCategory);
            Tag tag = EnsureTag (new TagInfo (new_tag_name), parent as Category);

            // Now we have the tag for this place, add the photo to it
            photo.AddTag (tag);
        }

        public bool Import (Photo photo, IBrowsableItem importing_from)
        {
            using (var metadata = Metadata.Parse (importing_from.DefaultVersion.Uri)) {
                // Copy Rating
                var rating = metadata.ImageTag.Rating;
                if (rating.HasValue) {
                    var rating_val = Math.Min (metadata.ImageTag.Rating.Value, 5);
                    photo.Rating = Math.Max (0, rating_val);
                }

                // Copy Keywords
                foreach (var keyword in metadata.ImageTag.Keywords) {
                    AddTagToPhoto (photo, keyword);
                }

                // XXX: We might want to copy more data.
            }
            return true;
        }

        public void Cancel()
        {
            // User have cancelled the import.
            // Remove all created tags
            while (tags_created.Count > 0)
                tag_store.Remove (tags_created.Pop());

            // Clear the tags_created array
            tags_created.Clear();
        }

        public void Finish()
        {
            // Clear the tags_created array, since we do not need it anymore.
            tags_created.Clear();
        }
    }
} // namespace
