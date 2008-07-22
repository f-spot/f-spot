// ContentWriter.cs
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
using System.Text;
using System.Net;
using System.IO;

namespace DPAP {

    internal class ContentWriter {


        private static void Write (ContentCodeBag bag, ContentNode node, BinaryWriter writer) {

            ContentCode code = bag.Lookup (node.Name);
            if (code.Equals (ContentCode.Zero)) {
                throw new ContentException ("Failed to get content code for: " + node.Name);
            }

            writer.Write (IPAddress.HostToNetworkOrder (code.Number));

            switch (code.Type) {
            case ContentType.Char:
                writer.Write (IPAddress.HostToNetworkOrder (1));
                writer.Write ((byte) node.Value);
                break;
            case ContentType.Short:
                writer.Write (IPAddress.HostToNetworkOrder (2));
                writer.Write (IPAddress.HostToNetworkOrder ((short) node.Value));
                break;
            case ContentType.SignedLong:
            case ContentType.Long:
                writer.Write (IPAddress.HostToNetworkOrder (4));
                writer.Write (IPAddress.HostToNetworkOrder ((int) node.Value));
                break;
            case ContentType.LongLong:
                writer.Write (IPAddress.HostToNetworkOrder (8));
                writer.Write (IPAddress.HostToNetworkOrder ((long) node.Value));
                break;
            case ContentType.String:
                byte[] data = Encoding.UTF8.GetBytes ((string) node.Value);
                writer.Write (IPAddress.HostToNetworkOrder (data.Length));
                writer.Write (data);
                break;
            case ContentType.Date:
                writer.Write (IPAddress.HostToNetworkOrder (4));
                writer.Write (IPAddress.HostToNetworkOrder (Utility.FromDateTime ((DateTime) node.Value)));
                break;
            case ContentType.Version:
                Version version = (Version) node.Value;
                writer.Write (IPAddress.HostToNetworkOrder (4));

                writer.Write ((short) IPAddress.HostToNetworkOrder ((short) version.Major));
                writer.Write ((byte) version.Minor);
                writer.Write ((byte) version.Build);
                break;
			case ContentType.FileData:
				Console.WriteLine("ContentWriter FileData!");
				ContentNode[] nodes = (ContentNode[]) node.Value;
				//writer.Write(IPAddress.HostToNetworkOrder (0));
				Console.WriteLine(nodes[0].Value);
				writer.Write(IPAddress.HostToNetworkOrder ((int)nodes[0].Value));
				Console.WriteLine("reading file!");
				FileInfo info = new FileInfo ((string)nodes[1].Value);

				FileStream stream = info.Open(FileMode.Open, FileAccess.Read, FileShare.Read);
				//writer.Write (client, stream, info.Length, offset);
				int offset = -1;
                using (BinaryReader reader = new BinaryReader (stream)) {
                    if (offset > 0) {
                        reader.BaseStream.Seek (offset, SeekOrigin.Begin);
                    }

                    long count = 0;
					long len = info.Length;
                    while (count < len) {
                        byte[] buf = reader.ReadBytes (Math.Min (8192, (int) len - (int) count));
                        if (buf.Length == 0) {
                            break;
                        }
                        
                        writer.Write (buf);
                        count += buf.Length;
                    }
                }
				break;
            case ContentType.Container:
                MemoryStream childStream = new MemoryStream ();
                BinaryWriter childWriter = new BinaryWriter (childStream);

                foreach (ContentNode child in (ContentNode[]) node.Value) {
                    Write (bag, child, childWriter);
                }

                childWriter.Flush ();
                byte[] bytes = childStream.GetBuffer ();
                int len = (int) childStream.Length;

                writer.Write (IPAddress.HostToNetworkOrder (len));
                writer.Write (bytes, 0, len);
                childWriter.Close ();
                break;
				
            default:
                Console.Error.WriteLine ("Cannot write node of type: " + code.Type);
                break;
            }
        }
        
        public static byte[] Write (ContentCodeBag bag, ContentNode node) {
            MemoryStream stream = new MemoryStream ();
            BinaryWriter writer = new BinaryWriter (stream);
            Write (bag, node, writer);
            writer.Flush ();

            byte[] buf = stream.GetBuffer ();
            long len = stream.Length;
            writer.Close ();

            byte[] ret = new byte[len];
            Array.Copy (buf, ret, len);
            return ret;
        }
    }
}
