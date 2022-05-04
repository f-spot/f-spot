//
// IntegerKeyedObjectQueryValue.cs
//
// Authors:
//   Scott Peterson <lunchtimemama@gmail.com>
//
// Copyright (C) 2008 Scott Peterson
//
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

namespace Hyena.Query
{
	public abstract class IntegerKeyedObjectQueryValue<T> : IntegerQueryValue where T : class
	{
		T object_value;

		public override void SetValue (long value)
		{
			object_value = null;
			base.SetValue (value);
		}

		public T ObjectValue {
			get {
				if (object_value == null) {
					object_value = Resolve ();
				}
				return object_value;
			}
		}

		protected abstract T Resolve ();
	}
}
