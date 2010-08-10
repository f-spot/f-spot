using System.Collections.Generic;
using System.Text;
using System;
using Hyena;

namespace FSpot.Utils
{
    public class UriList : List<SafeUri>
    {
        public UriList () : base()
        {
        }

        public UriList (string data)
        {
            LoadFromStrings (data.Split ('\n'));
        }

        public UriList (IEnumerable<SafeUri> uris)
        {
            foreach (SafeUri uri in uris) {
                Add (uri);
            }
        }

        private void LoadFromStrings (IEnumerable<string> items)
        {
            foreach (string i in items) {
                if (!i.StartsWith ("#")) {
                    SafeUri uri;
                    String s = i;
                    
                    if (i.EndsWith ("\r")) {
                        s = i.Substring (0, i.Length - 1);
                    }
                    
                    try {
                        uri = new SafeUri (s);
                    } catch {
                        continue;
                    }
                    Add (uri);
                }
            }
        }

        public void AddUnknown (string unknown)
        {
            Add (new SafeUri (unknown));
        }

        public override string ToString ()
        {
            StringBuilder list = new StringBuilder ();
            
            foreach (SafeUri uri in this) {
                if (uri == null)
                    break;
                
                list.Append (uri.ToString () + Environment.NewLine);
            }
            
            return list.ToString ();
        }
    }
}
