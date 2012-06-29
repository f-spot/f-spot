//
// FindNullableClashes.cs
//
// Author:
//   Ruben Vermeersch <ruben@savanne.be>
//   Peter Goetz <peter.gtz@gmail.com>
//
// Copyright (C) 2010 Novell, Inc.
// Copyright (C) 2010 Ruben Vermeersch
// Copyright (C) 2010 Peter Goetz
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
// THE SOFTWARE IS PROVIDED AS IS, WITHOUT WARRANTY OF ANY KIND,
// EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF
// MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND
// NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE
// LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION
// OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION
// WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
//

using System;
using System.Reflection;

using Hyena;

public class FindNullableClashes {
    public static void Main (string [] args) {
        Assembly asm = Assembly.LoadFrom (args[0]);
        foreach (Type type in asm.GetTypes ()) {
            foreach (FieldInfo field in type.GetFields ()) {
                var attrs = field.GetCustomAttributes (false);
                foreach (Attribute attr in attrs) {
                    if (attr is System.Xml.Serialization.XmlElementAttribute) {
                        var xattr = attr as System.Xml.Serialization.XmlElementAttribute;
                        var ftype = field.FieldType;
                        bool is_nullable = ftype.IsGenericType && ftype.GetGenericTypeDefinition() == typeof(Nullable<>);
                        if (xattr.IsNullable && ftype.IsValueType && field.DeclaringType == type && !is_nullable) {
                            Log.DebugFormat ("Possible clash for {0}/{1}", type.FullName, field.Name);
                            Log.DebugFormat ("   Type: {0}", field.FieldType.FullName);
                        }
                    }
                }
            }
        }
    }
}
