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
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

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
                            Logger.Log.DebugFormat ("Possible clash for {0}/{1}", type.FullName, field.Name);
                            Logger.Log.DebugFormat ("   Type: {0}", field.FieldType.FullName);
                        }
                    }
                }
            }
        }
    }
}
