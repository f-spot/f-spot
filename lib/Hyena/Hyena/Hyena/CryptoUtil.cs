//
// CryptoUtil.cs
//
// Author:
//   Aaron Bockover <abockover@novell.com>
//
// Copyright (C) 2008 Novell, Inc.
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
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND,
// EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF
// MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND
// NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE
// LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION
// OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION
// WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
//

using System;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using System.Security.Cryptography;

namespace Hyena
{
    public static class CryptoUtil
    {
        // A-Z is ignored on purpose
        private static Regex md5_regex = new Regex ("^[a-f0-9]{32}$", RegexOptions.Compiled);
        private static MD5 md5 = MD5.Create ();

        public static bool IsMd5Encoded (string text)
        {
            return text == null || text.Length != 32 ? false : md5_regex.IsMatch (text);
        }

        public static string Md5Encode (string text)
        {
            return Md5Encode (text, Encoding.ASCII);
        }

        public static string Md5Encode (string text, Encoding encoding)
        {
            if (String.IsNullOrEmpty (text)) {
                return String.Empty;
            }

            byte [] hash;
            lock (md5) {
                hash = md5.ComputeHash (encoding.GetBytes (text));
            }

            return ToHex (hash);
        }

        public static string Md5EncodeStream (Stream stream)
        {
            byte [] hash;
            lock (md5) {
                hash = md5.ComputeHash (stream);
            }

            return ToHex (hash);
        }

        private static string ToHex (byte [] hash)
        {
            StringBuilder shash = new StringBuilder ();
            for (int i = 0; i < hash.Length; i++) {
                shash.Append (hash[i].ToString ("x2"));
            }

            return shash.ToString ();
        }
    }
}
