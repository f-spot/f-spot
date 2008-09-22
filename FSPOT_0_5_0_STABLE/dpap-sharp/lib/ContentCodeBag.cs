// ContentCodeBag.cs
//
// Authors:
//   Andrzej Wytyczak-Partyka <iapart@gmail.com>
//   James Willcox <snorp@snorp.net>
//
// Copyright (C) 2008 Andrzej Wytyczak-Partyka
// Copyright (C) 2005  James Willcox <snorp@snorp.net>
//
// This program is free software; you can redistribute it and/or modify
// it under the terms of the GNU General Public License as published by
// the Free Software Foundation; either version 2 of the License, or
// (at your option) any later version.
//
// This program is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the
// GNU General Public License for more details.
// 
// You should have received a copy of the GNU General Public License
// along with this program; if not, write to the Free Software
// Foundation, Inc., 59 Temple Place, Suite 330, Boston, MA 02111-1307 USA
//
//
using System;
using System.Reflection;
using System.IO;
using System.Collections;
using System.Text;
using System.Net;

namespace DPAP {

    internal enum ContentType : short {
        Char = 1,
        SignedLong = 2,
        Short = 3,
        Long = 5,
        LongLong = 7,
        String = 9,
        Date = 10,
        Version = 11,
        Container = 12,
		FileData = 13
    }

    internal struct ContentCode {
        public int Number;
        public string Name;
        public ContentType Type;

        public static ContentCode Zero = new ContentCode ();
    }

    internal class ContentCodeBag {

        private const int ChunkLength = 8192;
        
        private static ContentCodeBag default_bag;
        private Hashtable codes = new Hashtable ();

        public static ContentCodeBag Default {
            get {
				Console.WriteLine ("getting default content codebag");
                if (default_bag == null) {

                    // this is crappy
                    foreach (string name in Assembly.GetExecutingAssembly ().GetManifestResourceNames ()) {
						Console.WriteLine ("resource " +name);
                        using (BinaryReader reader = new BinaryReader (Assembly.GetExecutingAssembly ().GetManifestResourceStream (name))) {
                            MemoryStream buf = new MemoryStream ();
                            byte [] bytes = null;

                            do {
                                bytes = reader.ReadBytes (ChunkLength);
                                buf.Write (bytes, 0, bytes.Length);
                            } while (bytes.Length == ChunkLength);

                            default_bag = ContentCodeBag.ParseCodes (buf.GetBuffer ());
							
							// FIXME this shouldn't be here, should work from the content-codes file, but doesn't
							default_bag.AddCode ("aply", "dpap.databasecontainers", ContentType.Container);
                        }
                    }
                }

                return default_bag;
            }
        }

        private ContentCodeBag () {
        }

        public ContentCode Lookup (int number) {
            if (codes.ContainsKey (number))
                return (ContentCode) codes [number];
            else
                return ContentCode.Zero;
        }

        public ContentCode Lookup (string name) {
            foreach (ContentCode code in codes.Values) {
                if (code.Name == name)
                    return code;
            }

            return ContentCode.Zero;
        }

        private static int GetIntFormat (string code) {
            return IPAddress.NetworkToHostOrder (BitConverter.ToInt32 (Encoding.ASCII.GetBytes (code), 0));
        }

        internal static string GetStringFormat (int code) {
            return Encoding.ASCII.GetString (BitConverter.GetBytes (IPAddress.HostToNetworkOrder (code)));
        }

        private void AddCode (string num, string name, ContentType type) {
            ContentCode code = new ContentCode ();
            code.Number = GetIntFormat (num);
            code.Name = name;
            code.Type = type;
			
            codes [code.Number] = code;
        }

        internal ContentNode ToNode () {
            ArrayList nodes = new ArrayList ();
            
            foreach (int number in codes.Keys) {
                ContentCode code = (ContentCode) codes [number];

                ArrayList contents = new ArrayList ();
                contents.Add (new ContentNode ("dmap.contentcodesnumber", code.Number));
                contents.Add (new ContentNode ("dmap.contentcodesname", code.Name));
                contents.Add (new ContentNode ("dmap.contentcodestype", code.Type));

                ContentNode dict = new ContentNode ("dmap.dictionary", contents);
                nodes.Add (dict);
            }

            ContentNode status = new ContentNode ("dmap.status", 200);
            return new ContentNode ("dmap.contentcodesresponse", status, nodes);
        }

        public static ContentCodeBag ParseCodes (byte [] buffer) {
            ContentCodeBag bag = new ContentCodeBag ();

            // add some codes to bootstrap us
            bag.AddCode ("mccr", "dmap.contentcodesresponse", ContentType.Container);
            bag.AddCode ("mdcl", "dmap.dictionary", ContentType.Container);
            bag.AddCode ("mcnm", "dmap.contentcodesnumber", ContentType.Long);
            bag.AddCode ("mcna", "dmap.contentcodesname", ContentType.String);
            bag.AddCode ("mcty", "dmap.contentcodestype", ContentType.Short);
            bag.AddCode ("mstt", "dmap.status", ContentType.Long);

            // some photo-specific codes
			// shouldn't be needed now
            bag.AddCode ("ppro", "dpap.protocolversion", ContentType.Long);
            bag.AddCode ("pret", "dpap.blah", ContentType.Container);
			bag.AddCode ("avdb", "dpap.serverdatabases", ContentType.Container);
			bag.AddCode ("aply", "dpap.databasecontainers", ContentType.Container);
			bag.AddCode ("abpl", "dpap.baseplaylist", ContentType.Char);
			bag.AddCode ("apso", "dpap.playlistsongs", ContentType.Container);
			bag.AddCode ("pasp", "dpap.aspectratio", ContentType.String);
			bag.AddCode ("adbs", "dpap.databasesongs", ContentType.Container);
			bag.AddCode ("picd", "dpap.creationdate", ContentType.Long);
			bag.AddCode ("pifs", "dpap.imagefilesize", ContentType.Long);
			bag.AddCode ("pwth", "dpap.imagepixelwidth", ContentType.Long);
			bag.AddCode ("phgt", "dpap.imagepixelheight", ContentType.Long);
			bag.AddCode ("pcmt", "dpap.imagecomments", ContentType.String);
			bag.AddCode ("prat", "dpap.imagerating", ContentType.Long);
			bag.AddCode ("pimf", "dpap.imagefilename", ContentType.String);
			bag.AddCode ("pfmt", "dpap.imageformat", ContentType.String);
			bag.AddCode ("plsz", "dpap.imagelargefilesize", ContentType.Long);
			bag.AddCode ("pfdt", "dpap.filedata", ContentType.FileData);
            
            

            ContentNode node = ContentParser.Parse (bag, buffer);

            foreach (ContentNode dictNode in (node.Value as ContentNode [])) {
				if (dictNode.Name != "dmap.dictionary") {
                    continue;
                }
                
                ContentCode code = new ContentCode ();
                
                foreach (ContentNode item in (dictNode.Value as ContentNode [])) {
                    switch (item.Name) {
                    case "dmap.contentcodesnumber":
                        code.Number = (int) item.Value;
                        break;
                    case "dmap.contentcodesname":
                        code.Name = (string) item.Value;
                        break;
                    case "dmap.contentcodestype":
                        code.Type = (ContentType) Enum.ToObject (typeof (ContentType), (short) item.Value);
                        break;
                    }
                }

                bag.codes [code.Number] = code;
            }
            return bag;
        }
    }
}
