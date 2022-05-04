//
// ObjectBinder.cs
//
// Author:
//   Aaron Bockover <abockover@novell.com>
//   Gabriel Burt <gburt@novell.com>
//
// Copyright 2010 Novell, Inc.
//
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using System;
using System.Reflection;

namespace Hyena.Data
{
	public class ObjectBinder : IDataBinder
	{
		PropertyInfo property_info;
		PropertyInfo sub_property_info;

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

		void EnsurePropertyInfo (string name, ref PropertyInfo prop, object obj)
		{
			if (prop == null || prop.ReflectedType != obj.GetType ()) {
				prop = obj.GetType ().GetProperty (name);
				if (prop == null) {
					throw new Exception (string.Format (
						"In {0}, type {1} does not have property {2}",
						this, obj.GetType (), name));
				}
			}
		}

		protected Type BoundType {
			get { return bound_object.GetType (); }
		}

		object bound_object;
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

		string property;
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
