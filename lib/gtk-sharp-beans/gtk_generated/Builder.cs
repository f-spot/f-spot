// This file was generated by the Gtk# code generator.
// Any changes made will be lost if regenerated.

namespace GtkBeans
{

	using System;
	using System.Collections;
	using System.Runtime.InteropServices;

	#region Autogenerated code
	public class Builder : GLib.Object
	{

		[Obsolete]
		protected Builder (GLib.GType gtype) : base (gtype) { }
		public Builder (IntPtr raw) : base (raw) { }

		[DllImport ("libgtk-win32-2.0-0.dll", CallingConvention = CallingConvention.Cdecl)]
		static extern IntPtr gtk_builder_new ();

		public Builder () : base (IntPtr.Zero)
		{
			if (GetType () != typeof (Builder)) {
				CreateNativeObject (Array.Empty<string> (), Array.Empty<GLib.Value> ());
				return;
			}
			Raw = gtk_builder_new ();
		}

		[DllImport ("libgtk-win32-2.0-0.dll", CallingConvention = CallingConvention.Cdecl)]
		static extern IntPtr gtk_builder_get_translation_domain (IntPtr raw);

		[DllImport ("libgtk-win32-2.0-0.dll", CallingConvention = CallingConvention.Cdecl)]
		static extern void gtk_builder_set_translation_domain (IntPtr raw, IntPtr domain);

		[GLib.Property ("translation-domain")]
		public string TranslationDomain {
			get {
				IntPtr raw_ret = gtk_builder_get_translation_domain (Handle);
				string ret = GLib.Marshaller.Utf8PtrToString (raw_ret);
				return ret;
			}
			set {
				IntPtr native_value = GLib.Marshaller.StringToPtrGStrdup (value);
				gtk_builder_set_translation_domain (Handle, native_value);
				GLib.Marshaller.Free (native_value);
			}
		}

		[DllImport ("libgtk-win32-2.0-0.dll", CallingConvention = CallingConvention.Cdecl)]
		static extern IntPtr gtk_builder_get_object (IntPtr raw, IntPtr name);

		public GLib.Object GetObject (string name)
		{
			IntPtr native_name = GLib.Marshaller.StringToPtrGStrdup (name);
			IntPtr raw_ret = gtk_builder_get_object (Handle, native_name);
			var ret = GLib.Object.GetObject (raw_ret);
			GLib.Marshaller.Free (native_name);
			return ret;
		}

		[DllImport ("libgtk-win32-2.0-0.dll", CallingConvention = CallingConvention.Cdecl)]
		static extern unsafe bool gtk_builder_value_from_string (IntPtr raw, IntPtr pspec, IntPtr str1ng, IntPtr value, out IntPtr error);

		public unsafe bool ValueFromString (IntPtr pspec, string str1ng, GLib.Value value)
		{
			IntPtr native_str1ng = GLib.Marshaller.StringToPtrGStrdup (str1ng);
			IntPtr native_value = GLib.Marshaller.StructureToPtrAlloc (value);
			IntPtr error = IntPtr.Zero;
			bool raw_ret = gtk_builder_value_from_string (Handle, pspec, native_str1ng, native_value, out error);
			bool ret = raw_ret;
			GLib.Marshaller.Free (native_str1ng);
			value = (GLib.Value)Marshal.PtrToStructure (native_value, typeof (GLib.Value));
			Marshal.FreeHGlobal (native_value);
			if (error != IntPtr.Zero) throw new GLib.GException (error);
			return ret;
		}

		[DllImport ("libgtk-win32-2.0-0.dll", CallingConvention = CallingConvention.Cdecl)]
		static extern unsafe uint gtk_builder_add_from_string (IntPtr raw, IntPtr buffer, UIntPtr length, out IntPtr error);

		public unsafe uint AddFromString (string buffer)
		{
			IntPtr native_buffer = GLib.Marshaller.StringToPtrGStrdup (buffer);
			IntPtr error = IntPtr.Zero;
			uint raw_ret = gtk_builder_add_from_string (Handle, native_buffer, new UIntPtr ((ulong)System.Text.Encoding.UTF8.GetByteCount (buffer)), out error);
			uint ret = raw_ret;
			GLib.Marshaller.Free (native_buffer);
			if (error != IntPtr.Zero) throw new GLib.GException (error);
			return ret;
		}

		[DllImport ("libgtk-win32-2.0-0.dll", CallingConvention = CallingConvention.Cdecl)]
		static extern void gtk_builder_connect_signals_full (IntPtr raw, GtkBeansSharp.BuilderConnectFuncNative func, IntPtr user_data);

		public void ConnectSignalsFull (GtkBeans.BuilderConnectFunc func)
		{
			var func_wrapper = new GtkBeansSharp.BuilderConnectFuncWrapper (func);
			gtk_builder_connect_signals_full (Handle, func_wrapper.NativeDelegate, IntPtr.Zero);
		}

		[DllImport ("libgtk-win32-2.0-0.dll", CallingConvention = CallingConvention.Cdecl)]
		static extern unsafe bool gtk_builder_value_from_string_type (IntPtr raw, IntPtr type, IntPtr str1ng, IntPtr value, out IntPtr error);

		public unsafe bool ValueFromStringType (GLib.GType type, string str1ng, GLib.Value value)
		{
			IntPtr native_str1ng = GLib.Marshaller.StringToPtrGStrdup (str1ng);
			IntPtr native_value = GLib.Marshaller.StructureToPtrAlloc (value);
			IntPtr error = IntPtr.Zero;
			bool raw_ret = gtk_builder_value_from_string_type (Handle, type.Val, native_str1ng, native_value, out error);
			bool ret = raw_ret;
			GLib.Marshaller.Free (native_str1ng);
			value = (GLib.Value)Marshal.PtrToStructure (native_value, typeof (GLib.Value));
			Marshal.FreeHGlobal (native_value);
			if (error != IntPtr.Zero) throw new GLib.GException (error);
			return ret;
		}

		[DllImport ("libgtk-win32-2.0-0.dll", CallingConvention = CallingConvention.Cdecl)]
		static extern int gtk_builder_error_quark ();

		public static int ErrorQuark ()
		{
			int raw_ret = gtk_builder_error_quark ();
			int ret = raw_ret;
			return ret;
		}

		[DllImport ("libgtk-win32-2.0-0.dll", CallingConvention = CallingConvention.Cdecl)]
		static extern IntPtr gtk_builder_get_objects (IntPtr raw);

		public GLib.SList Objects {
			get {
				IntPtr raw_ret = gtk_builder_get_objects (Handle);
				var ret = new GLib.SList (raw_ret);
				return ret;
			}
		}

		[DllImport ("libgtk-win32-2.0-0.dll", CallingConvention = CallingConvention.Cdecl)]
		static extern void gtk_builder_connect_signals (IntPtr raw, IntPtr user_data);

		public void ConnectSignals (IntPtr user_data)
		{
			gtk_builder_connect_signals (Handle, user_data);
		}

		[DllImport ("libgtk-win32-2.0-0.dll", CallingConvention = CallingConvention.Cdecl)]
		static extern IntPtr gtk_builder_get_type ();

		public static new GLib.GType GType {
			get {
				IntPtr raw_ret = gtk_builder_get_type ();
				var ret = new GLib.GType (raw_ret);
				return ret;
			}
		}

		[DllImport ("libgtk-win32-2.0-0.dll", CallingConvention = CallingConvention.Cdecl)]
		static extern unsafe uint gtk_builder_add_objects_from_file (IntPtr raw, IntPtr filename, IntPtr object_ids, out IntPtr error);

		public unsafe uint AddObjectsFromFile (string filename, string object_ids)
		{
			IntPtr native_filename = GLib.Marshaller.StringToPtrGStrdup (filename);
			IntPtr error = IntPtr.Zero;
			uint raw_ret = gtk_builder_add_objects_from_file (Handle, native_filename, GLib.Marshaller.StringToPtrGStrdup (object_ids), out error);
			uint ret = raw_ret;
			GLib.Marshaller.Free (native_filename);
			if (error != IntPtr.Zero) throw new GLib.GException (error);
			return ret;
		}

		[DllImport ("libgtk-win32-2.0-0.dll", CallingConvention = CallingConvention.Cdecl)]
		static extern unsafe uint gtk_builder_add_objects_from_string (IntPtr raw, IntPtr buffer, UIntPtr length, IntPtr object_ids, out IntPtr error);

		public unsafe uint AddObjectsFromString (string buffer, string object_ids)
		{
			IntPtr native_buffer = GLib.Marshaller.StringToPtrGStrdup (buffer);
			IntPtr error = IntPtr.Zero;
			uint raw_ret = gtk_builder_add_objects_from_string (Handle, native_buffer, new UIntPtr ((ulong)System.Text.Encoding.UTF8.GetByteCount (buffer)), GLib.Marshaller.StringToPtrGStrdup (object_ids), out error);
			uint ret = raw_ret;
			GLib.Marshaller.Free (native_buffer);
			if (error != IntPtr.Zero) throw new GLib.GException (error);
			return ret;
		}

		[DllImport ("libgtk-win32-2.0-0.dll", CallingConvention = CallingConvention.Cdecl)]
		static extern IntPtr gtk_builder_get_type_from_name (IntPtr raw, IntPtr type_name);

		public GLib.GType GetTypeFromName (string type_name)
		{
			IntPtr native_type_name = GLib.Marshaller.StringToPtrGStrdup (type_name);
			IntPtr raw_ret = gtk_builder_get_type_from_name (Handle, native_type_name);
			var ret = new GLib.GType (raw_ret);
			GLib.Marshaller.Free (native_type_name);
			return ret;
		}

		[DllImport ("libgtk-win32-2.0-0.dll", CallingConvention = CallingConvention.Cdecl)]
		static extern unsafe uint gtk_builder_add_from_file (IntPtr raw, IntPtr filename, out IntPtr error);

		public unsafe uint AddFromFile (string filename)
		{
			IntPtr native_filename = GLib.Marshaller.StringToPtrGStrdup (filename);
			IntPtr error = IntPtr.Zero;
			uint raw_ret = gtk_builder_add_from_file (Handle, native_filename, out error);
			uint ret = raw_ret;
			GLib.Marshaller.Free (native_filename);
			if (error != IntPtr.Zero) throw new GLib.GException (error);
			return ret;
		}


		static Builder ()
		{
			GtkSharp.GtkbeansSharp.ObjectManager.Initialize ();
		}
		#endregion
		#region Customized extensions
#line 1 "Builder.custom"
		// Builder.custom - customizations to Gtk.Builder
		//
		// Authors: Stephane Delcroix  <stephane@delcroix.org>
		// The biggest part of this code is adapted from glade#, by
		//	Ricardo Fernández Pascual <ric@users.sourceforge.net>
		//	Rachel Hestilow <hestilow@ximian.com>
		//
		// Copyright (c) 2008, 2009 Novell, Inc.
		//
		// This program is free software; you can redistribute it and/or
		// modify it under the terms of version 2 of the Lesser GNU General
		// Public License as published by the Free Software Foundation.
		//
		// This program is distributed in the hope that it will be useful,
		// but WITHOUT ANY WARRANTY; without even the implied warranty of
		// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the GNU
		// Lesser General Public License for more details.
		//
		// You should have received a copy of the GNU Lesser General Public
		// License along with this program; if not, write to the
		// Free Software Foundation, Inc., 59 Temple Place - Suite 330,
		// Boston, MA 02111-1307, USA.

		[System.Serializable]
		public class HandlerNotFoundException : SystemException
		{
			string handler_name;
			string signal_name;
			System.Reflection.EventInfo evnt;
			Type delegate_type;

			public HandlerNotFoundException (string handler_name, string signal_name,
							 System.Reflection.EventInfo evnt, Type delegate_type)
				: this (handler_name, signal_name, evnt, delegate_type, null)
			{
			}

			public HandlerNotFoundException (string handler_name, string signal_name,
							 System.Reflection.EventInfo evnt, Type delegate_type, Exception inner)
				: base ("No handler " + handler_name + " found for signal " + signal_name,
					inner)
			{
				this.handler_name = handler_name;
				this.signal_name = signal_name;
				this.evnt = evnt;
				this.delegate_type = delegate_type;
			}

			public HandlerNotFoundException (string message, string handler_name, string signal_name,
							 System.Reflection.EventInfo evnt, Type delegate_type)
				: base ((message != null) ? message : "No handler " + handler_name + " found for signal " + signal_name,
					null)
			{
				this.handler_name = handler_name;
				this.signal_name = signal_name;
				this.evnt = evnt;
				this.delegate_type = delegate_type;
			}

			protected HandlerNotFoundException (System.Runtime.Serialization.SerializationInfo info, System.Runtime.Serialization.StreamingContext context)
				: base (info, context)
			{
				handler_name = info.GetString ("HandlerName");
				signal_name = info.GetString ("SignalName");
				evnt = info.GetValue ("Event", typeof (System.Reflection.EventInfo)) as System.Reflection.EventInfo;
				delegate_type = info.GetValue ("DelegateType", typeof (Type)) as Type;
			}

			public string HandlerName {
				get {
					return handler_name;
				}
			}

			public string SignalName {
				get {
					return signal_name;
				}
			}

			public System.Reflection.EventInfo Event {
				get {
					return evnt;
				}
			}

			public Type DelegateType {
				get {
					return delegate_type;
				}
			}

			public override void GetObjectData (System.Runtime.Serialization.SerializationInfo info, System.Runtime.Serialization.StreamingContext context)
			{
				base.GetObjectData (info, context);
				info.AddValue ("HandlerName", handler_name);
				info.AddValue ("SignalName", signal_name);
				info.AddValue ("Event", evnt);
				info.AddValue ("DelegateType", delegate_type);
			}
		}


		[AttributeUsage (AttributeTargets.Field)]
		public class ObjectAttribute : Attribute
		{
			string name;
			bool specified;

			public ObjectAttribute (string name)
			{
				specified = true;
				this.name = name;
			}

			public ObjectAttribute ()
			{
				specified = false;
			}

			public string Name {
				get { return name; }
			}

			public bool Specified {
				get { return specified; }
			}
		}

		public IntPtr GetRawObject (string name)
		{
			IntPtr native_name = GLib.Marshaller.StringToPtrGStrdup (name);
			IntPtr raw_ret = gtk_builder_get_object (Handle, native_name);
			GLib.Marshaller.Free (native_name);
			return raw_ret;
		}

		public Builder (System.IO.Stream s) : this (s, null)
		{
		}

		public Builder (System.IO.Stream s, string translation_domain)
		{
			if (s == null)
				throw new ArgumentNullException (nameof (s));

			int size = (int)s.Length;
			byte[] buffer = new byte[size];
			s.Read (buffer, 0, size);
			s.Close ();

			AddFromString (System.Text.Encoding.UTF8.GetString (buffer));

			TranslationDomain = translation_domain;
		}

		public Builder (string resource_name) : this (resource_name, null)
		{
		}

		public Builder (string resource_name, string translation_domain) : this (System.Reflection.Assembly.GetEntryAssembly (), resource_name, translation_domain)
		{
		}

		public Builder (System.Reflection.Assembly assembly, string resource_name, string translation_domain) : this ()
		{
			if (GetType () != typeof (Builder))
				throw new InvalidOperationException ("Cannot chain to this constructor from subclasses.");

			if (assembly == null)
				assembly = System.Reflection.Assembly.GetCallingAssembly ();

			System.IO.Stream s = assembly.GetManifestResourceStream (resource_name);
			if (s == null)
				throw new ArgumentException ("Cannot get resource file '" + resource_name + "'",
								 nameof (resource_name));

			int size = (int)s.Length;
			byte[] buffer = new byte[size];
			s.Read (buffer, 0, size);
			s.Close ();

			AddFromString (System.Text.Encoding.UTF8.GetString (buffer));

			TranslationDomain = translation_domain;
		}

		public void Autoconnect (object handler)
		{
			BindFields (handler);
			(new SignalConnector (this, handler)).ConnectSignals ();
		}

		public void Autoconnect (Type handler_class)
		{
			BindFields (handler_class);
			(new SignalConnector (this, handler_class)).ConnectSignals ();
		}

		class SignalConnector
		{
			Builder builder;
			Type handler_type;
			object handler;

			public SignalConnector (Builder builder, object handler)
			{
				this.builder = builder;
				this.handler = handler;
				handler_type = handler.GetType ();
			}

			public SignalConnector (Builder builder, Type handler_type)
			{
				this.builder = builder;
				this.handler = null;
				this.handler_type = handler_type;
			}

			[DllImport ("libgtk-win32-2.0-0.dll", CallingConvention = CallingConvention.Cdecl)]
			static extern void gtk_builder_connect_signals_full (IntPtr raw, GtkBeansSharp.BuilderConnectFuncNative func, IntPtr user_data);

			public void ConnectSignals ()
			{
				var func_wrapper = new GtkBeansSharp.BuilderConnectFuncWrapper (new GtkBeans.BuilderConnectFunc (ConnectFunc));
				gtk_builder_connect_signals_full (builder.Handle, func_wrapper.NativeDelegate, IntPtr.Zero);
			}

			public void ConnectFunc (Builder builder, GLib.Object objekt, string signal_name, string handler_name, GLib.Object connect_object, GLib.ConnectFlags flags)
			{
				/* search for the event to connect */
				System.Reflection.MemberInfo[] evnts = objekt.GetType ().
					FindMembers (System.Reflection.MemberTypes.Event,
						 System.Reflection.BindingFlags.Instance
						 | System.Reflection.BindingFlags.Static
						 | System.Reflection.BindingFlags.Public
						 | System.Reflection.BindingFlags.NonPublic,
						 new System.Reflection.MemberFilter (SignalFilter), signal_name);
				foreach (System.Reflection.EventInfo ei in evnts) {
					bool connected = false;
					System.Reflection.MethodInfo add = ei.GetAddMethod ();
					System.Reflection.ParameterInfo[] addpi = add.GetParameters ();
					if (addpi.Length == 1) { /* this should be always true, unless there's something broken */
						Type delegate_type = addpi[0].ParameterType;

						/* look for an instance method */
						if (connect_object != null || handler != null)
							try {
								var d = Delegate.CreateDelegate (delegate_type, connect_object != null ? connect_object : handler, handler_name);
								add.Invoke (objekt, new object[] { d });
								connected = true;
							} catch (ArgumentException) { /* ignore if there is not such instance method */
							}

						/* look for a static method if no instance method has been found */
						if (!connected && handler_type != null)
							try {
								var d = Delegate.CreateDelegate (delegate_type, handler_type, handler_name);
								add.Invoke (objekt, new object[] { d });
								connected = true;
							} catch (ArgumentException) { /* ignore if there is not such static method */
							}

						if (!connected) {
							string msg = ExplainError (ei.Name, delegate_type, handler_type, handler_name);
							throw new HandlerNotFoundException (msg, handler_name, signal_name, ei, delegate_type);
						}
					}
				}
			}

			static bool SignalFilter (System.Reflection.MemberInfo m, object filterCriteria)
			{
				string signame = (filterCriteria as string);
				object[] attrs = m.GetCustomAttributes (typeof (GLib.SignalAttribute), false);
				if (attrs.Length > 0) {
					foreach (GLib.SignalAttribute a in attrs) {
						if (signame == a.CName) {
							return true;
						}
					}
					return false;
				} else {
					/* this tries to match the names when no attibutes are present.
					   It is only a fallback. */
					signame = signame.ToLower ().Replace ("_", "");
					string evname = m.Name.ToLower ();
					return signame == evname;
				}
			}

			static string GetSignature (System.Reflection.MethodInfo method)
			{
				if (method == null)
					return null;

				System.Reflection.ParameterInfo[] parameters = method.GetParameters ();
				var sb = new System.Text.StringBuilder ();
				sb.Append ('(');
				foreach (System.Reflection.ParameterInfo info in parameters) {
					sb.Append (info.ParameterType.ToString ());
					sb.Append (',');
				}
				if (sb.Length != 0)
					sb.Length--;

				sb.Append (')');
				return sb.ToString ();
			}

			static string GetSignature (Type delegate_type)
			{
				System.Reflection.MethodInfo method = delegate_type.GetMethod ("Invoke");
				return GetSignature (method);
			}

			const System.Reflection.BindingFlags flags = System.Reflection.BindingFlags.NonPublic |
							System.Reflection.BindingFlags.Public |
							System.Reflection.BindingFlags.Static |
							System.Reflection.BindingFlags.Instance;
			static string GetSignature (Type klass, string method_name)
			{
				try {
					System.Reflection.MethodInfo method = klass.GetMethod (method_name, flags);
					return GetSignature (method);
				} catch {
					// May be more than one method with that name and none matches
					return null;
				}
			}


			static string ExplainError (string event_name, Type deleg, Type klass, string method)
			{
				if (deleg == null || klass == null || method == null)
					return null;

				var sb = new System.Text.StringBuilder ();
				string expected = GetSignature (deleg);
				string actual = GetSignature (klass, method);
				if (actual == null)
					return null;
				sb.AppendFormat ("The handler for the event {0} should take '{1}', " +
					"but the signature of the provided handler ('{2}') is '{3}'\n",
					event_name, expected, method, actual);
				return sb.ToString ();
			}

		}


		void BindFields (object target)
		{
			BindFields (target, target.GetType ());
		}

		void BindFields (Type type)
		{
			BindFields (null, type);
		}

		void BindFields (object target, Type type)
		{
			System.Reflection.BindingFlags flags = System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.DeclaredOnly;
			if (target != null)
				flags |= System.Reflection.BindingFlags.Instance;
			else
				flags |= System.Reflection.BindingFlags.Static;

			do {
				System.Reflection.FieldInfo[] fields = type.GetFields (flags);
				if (fields == null)
					return;

				foreach (System.Reflection.FieldInfo field in fields) {
					object[] attrs = field.GetCustomAttributes (typeof (ObjectAttribute), false);
					if (attrs == null || attrs.Length == 0)
						continue;
					// The widget to field binding must be 1:1, so only check
					// the first attribute.
					var attr = (ObjectAttribute)attrs[0];
					GLib.Object gobject;
					if (attr.Specified)
						gobject = GetObject (attr.Name);
					else
						gobject = GetObject (field.Name);

					if (gobject != null)
						try {
							field.SetValue (target, gobject, flags, null, null);
						} catch (Exception) {
							Console.WriteLine ("Unable to set value for field " + field.Name);
							throw;
						}
				}
				type = type.BaseType;
			}
			while (type != typeof (object) && type != null);
		}

		#endregion
	}
}
