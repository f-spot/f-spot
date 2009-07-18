// Hasher.cs
// 
// Author:
//   James Willcox <snorp@snorp.net>
//
// Copyright (C) 2005  James Willcox <snorp@snorp.net>
// 
// This library is free software; you can redistribute it and/or
// modify it under the terms of the GNU Lesser General Public
// License as published by the Free Software Foundation; either
// version 2.1 of the License, or (at your option) any later version.
// 
// This library is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the GNU
// Lesser General Public License for more details.
// 
// You should have received a copy of the GNU Lesser General Public
// License along with this library; if not, write to the Free Software
// Foundation, Inc., 51 Franklin Street, Fifth Floor, Boston, MA  02110-1301  USA
// 
// Copyright (C) 2004 David Hammerton <david@crazney.net>
// Copyright (C) 2005 Jon Lech Johansen <jon@nanocrew.net>
// 
// Permission is hereby granted, free of charge, to any person
// obtaining a copy of this software and associated documentation
// files (the "Software"), to deal in the Software without
// restriction, including without limitation the rights to use,
// copy, modify, merge, publish, distribute, sublicense, and/or
// sell copies of the Software, and to permit persons to whom the
// Software is furnished to do so, subject to the following conditions:
// 
// The above copyright notice and this permission notice shall be
// included in all copies or substantial portions of the Software.
// 
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND,
// EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES
// OF MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND
// NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT
// HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY,
// WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING
// FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR
// OTHER DEALINGS IN THE SOFTWARE.
// 

using System;
using System.Text;

namespace DPAP {
    
    internal class Hasher
    {
        private static byte [] _hasht42 = null;
        private static byte [] _hasht45 = null;

        private static byte [] _hexchars =
        Encoding.ASCII.GetBytes ( "0123456789ABCDEF" );
        private static byte [] _copyright = Convert.FromBase64String (
                                                                     "Q29weXJpZ2h0IDIwMDMgQXBwbGUgQ29tcHV0ZXIsIEluYy4=" );

        private static void HashToString ( byte [] hash, byte [] str, int offset )
        {
            for ( int i = 0; i < hash.Length; i++ )
            {
                byte tmp = hash [ i ];
                str [ i * 2 + 1 + offset ] = _hexchars [ tmp & 0xF ];
                str [ i * 2 + 0 + offset ] = _hexchars [ (tmp >> 4) & 0xF ];
            }
        }

        private static void TransformString ( BrokenMD5 md5, string str, bool final )
        {
            byte [] tmp = Encoding.ASCII.GetBytes ( str );

            if ( final )
                md5.TransformFinalBlock ( tmp, 0, tmp.Length );
            else
                md5.TransformBlock ( tmp, 0, tmp.Length, tmp, 0 );
        }

        private static void GenerateTable42 ()
        {
            int i;

            _hasht42 = new byte [ 256 * 32 ];

            for ( i = 0; i < 256; i++ )
            {
                BrokenMD5 md5 = new BrokenMD5 ( 0 );

                if ( ( i & 0x80 ) != 0 )
                    TransformString ( md5, "Accept-Language", false );
                else
                    TransformString ( md5, "user-agent", false );

                if ( ( i & 0x40 ) != 0 )
                    TransformString ( md5, "max-age", false );
                else
                    TransformString ( md5, "Authorization", false );

                if ( ( i & 0x20 ) != 0 )
                    TransformString ( md5, "Client-DAAP-Version", false );
                else
                    TransformString ( md5, "Accept-Encoding", false );

                if ( ( i & 0x10 ) != 0 )
                    TransformString ( md5, "daap.protocolversion", false );
                else
                    TransformString ( md5, "daap.songartist", false );

                if ( ( i & 0x08 ) != 0 )
                    TransformString ( md5, "daap.songcomposer", false );
                else
                    TransformString ( md5, "daap.songdatemodified", false );

                if ( ( i & 0x04 ) != 0 )
                    TransformString ( md5, "daap.songdiscnumber", false );
                else
                    TransformString ( md5, "daap.songdisabled", false );

                if ( ( i & 0x02 ) != 0 )
                    TransformString ( md5, "playlist-item-spec", false );
                else
                    TransformString ( md5, "revision-number", false );

                if ( ( i & 0x01 ) != 0 )
                    TransformString ( md5, "session-id", true );
                else
                    TransformString ( md5, "content-codes", true );

                HashToString ( md5.Hash, _hasht42, i * 32 );
            }
        }

        private static void GenerateTable45 ()
        {
            int i;

            _hasht45 = new byte [ 256 * 32 ];

            for ( i = 0; i < 256; i++ )
            {
                BrokenMD5 md5 = new BrokenMD5 ( 1 );

                if ( ( i & 0x40 ) != 0 )
                    TransformString ( md5, "eqwsdxcqwesdc", false );
                else
                    TransformString ( md5, "op [;lm,piojkmn", false );

                if ( ( i & 0x20 ) != 0 )
                    TransformString ( md5, "876trfvb 34rtgbvc", false );
                else
                    TransformString ( md5, "=-0ol.,m3ewrdfv", false );

                if ( ( i & 0x10 ) != 0 )
                    TransformString ( md5, "87654323e4rgbv ", false );
                else
                    TransformString ( md5, "1535753690868867974342659792", false );

                if ( ( i & 0x08 ) != 0 )
                    TransformString ( md5, "Song Name", false );
                else
                    TransformString ( md5, "DAAP-CLIENT-ID:", false );

                if ( ( i & 0x04 ) != 0 )
                    TransformString ( md5, "111222333444555", false );
                else
                    TransformString ( md5, "4089961010", false );

                if ( ( i & 0x02 ) != 0 )
                    TransformString ( md5, "playlist-item-spec", false );
                else
                    TransformString ( md5, "revision-number", false );

                if ( ( i & 0x01 ) != 0 )
                    TransformString ( md5, "session-id", false );
                else
                    TransformString ( md5, "content-codes", false );

                if ( ( i & 0x80 ) != 0 )
                    TransformString ( md5, "IUYHGFDCXWEDFGHN", true );
                else
                    TransformString ( md5, "iuytgfdxwerfghjm", true );

                HashToString ( md5.Hash, _hasht45, i * 32 );
            }
        }

        public static string GenerateHash (int version_major, string url,
                                          int hash_select, int request_id )
        {
            if ( _hasht42 == null )
                GenerateTable42 ();
            if ( _hasht45 == null )
                GenerateTable45 ();

            byte [] hashtable = (version_major == 3) ? _hasht45 : _hasht42;
            BrokenMD5 md5 = new BrokenMD5 ( (version_major == 3) ? 1 : 0 );
            byte [] hash = new byte [ 32 ];

            byte [] tmp = Encoding.ASCII.GetBytes ( url );
            md5.TransformBlock ( tmp, 0, tmp.Length, tmp, 0 );
            md5.TransformBlock ( _copyright, 0, _copyright.Length, _copyright, 0 );

            if ( request_id > 0 && version_major == 3 )
            {
                md5.TransformBlock ( hashtable, hash_select * 32, 32,
                                    hashtable, hash_select * 32 );
                tmp = Encoding.ASCII.GetBytes ( request_id.ToString () );
                md5.TransformFinalBlock ( tmp, 0, tmp.Length );
            }
            else
            {
                md5.TransformFinalBlock ( hashtable, hash_select * 32, 32 );
            }

            HashToString ( md5.Hash, hash, 0 );

            return Encoding.ASCII.GetString ( hash );
        }
    }
}
