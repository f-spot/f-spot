//
// ObjectBinder.cs
//
// Author:
//   Aaron Bockover <abockover@novell.com>
//   Gabriel Burt <gburt@novell.com>
//
// Copyright 2010 Novell, Inc.
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
using System.Reflection;

namespace Hyena.Data
{
    public class ObjectBinder : IDataBinder
    {
        private PropertyInfo property_info;
        private PropertyInfo sub_property_info;

        public void Bind (object item)
        {
            BindDataItem (item);
        }

        public virtual void BindDataItem (object item)
        {
            if (item == null) {
                BoundObjectParent = null;
                bound_object = null;
                return;
            }

            BoundObjectParent = item;

            if (Property != null) {
                EnsurePropertyInfo (Property, ref property_info, BoundObjectParent);
                bound_object = property_info.GetValue (BoundObjectParent, null);

                if (SubProperty != null) {
                    EnsurePropertyInfo (SubProperty, ref sub_property_info, bound_object);
                    bound_object = sub_property_info.GetValue (bound_object, null);
                }
            } else {
                bound_object = BoundObjectParent;
            }
        }

        private void EnsurePropertyInfo (string name, ref PropertyInfo prop, object obj)
        {
            if (prop == null || prop.ReflectedType != obj.GetType ()) {
                prop = obj.GetType ().GetProperty (name);
                if (prop == null) {
                    throw new Exception (String.Format (
                        "In {0}, type {1} does not have property {2}",
                        this, obj.GetType (), name));
                }
            }
        }

        protected Type BoundType {
            get { return bound_object.GetType (); }
        }

        private object bound_object;
        public object BoundObject {
            get { return bound_object; }
            set {
                if (Property != null) {
                    EnsurePropertyInfo (Property, ref property_info, BoundObjectParent);
                    property_info.SetValue (BoundObjectParent, value, null);
                }
            }
        }

        public object BoundObjectParent { get; private set; }

        private string property;
        public string Property {
            get { return property; }
            set {
                property = value;
                if (value != null) {
                    int i = value.IndexOf (".");
                    if (i != -1) {
                        property = value.Substring (0, i);
                        SubProperty = value.Substring (i + 1, value.Length - i - 1);
                    }
                }
            }
        }

        public string SubProperty { get; set; }
    }
}
