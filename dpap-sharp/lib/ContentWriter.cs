/*
 * daap-sharp
 * Copyright (C) 2005  James Willcox <snorp@snorp.net>
 * 
 * This library is free software; you can redistribute it and/or
 * modify it under the terms of the GNU Lesser General Public
 * License as published by the Free Software Foundation; either
 * version 2.1 of the License, or (at your option) any later version.
 * 
 * This library is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the GNU
 * Lesser General Public License for more details.
 * 
 * You should have received a copy of the GNU Lesser General Public
 * License along with this library; if not, write to the Free Software
 * Foundation, Inc., 51 Franklin Street, Fifth Floor, Boston, MA  02110-1301  USA
 */
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
