// This file was generated by the Gtk# code generator.
// Any changes made will be lost if regenerated.

namespace GLib {

	using System;
	using System.Runtime.InteropServices;

#region Autogenerated code
	public class AsyncInitableAdapter : GLib.GInterfaceAdapter, GLib.AsyncInitable {

		static AsyncInitableIface iface;

		struct AsyncInitableIface {
			public IntPtr gtype;
			public IntPtr itype;

			public InitAsyncDelegate init_async;
			public InitFinishDelegate init_finish;
		}

		static AsyncInitableAdapter ()
		{
			GLib.GType.Register (_gtype, typeof(AsyncInitableAdapter));
			iface.init_async = new InitAsyncDelegate (InitAsyncCallback);
			iface.init_finish = new InitFinishDelegate (InitFinishCallback);
		}


		[UnmanagedFunctionPointer (CallingConvention.Cdecl)]
		delegate void InitAsyncDelegate (IntPtr initable, int io_priority, IntPtr cancellable, GLibSharp.AsyncReadyCallbackNative cb, IntPtr user_data);

		static void InitAsyncCallback (IntPtr initable, int io_priority, IntPtr cancellable, GLibSharp.AsyncReadyCallbackNative cb, IntPtr user_data)
		{
			try {
				GLib.AsyncInitableImplementor __obj = GLib.Object.GetObject (initable, false) as GLib.AsyncInitableImplementor;
				GLibSharp.AsyncReadyCallbackInvoker cb_invoker = new GLibSharp.AsyncReadyCallbackInvoker (cb, user_data);
				__obj.InitAsync (io_priority, GLib.Object.GetObject(cancellable) as GLib.Cancellable, cb_invoker.Handler);
			} catch (Exception e) {
				GLib.ExceptionManager.RaiseUnhandledException (e, false);
			}
		}

		[UnmanagedFunctionPointer (CallingConvention.Cdecl)]
		delegate bool InitFinishDelegate (IntPtr initable, IntPtr res, out IntPtr error);

		static bool InitFinishCallback (IntPtr initable, IntPtr res, out IntPtr error)
		{
			error = IntPtr.Zero;

			try {
				GLib.AsyncInitableImplementor __obj = GLib.Object.GetObject (initable, false) as GLib.AsyncInitableImplementor;
				bool __result = __obj.InitFinish (GLib.AsyncResultAdapter.GetObject (res, false));
				return __result;
			} catch (Exception e) {
				GLib.ExceptionManager.RaiseUnhandledException (e, true);
				// NOTREACHED: above call does not return.
				throw e;
			}
		}
		static void Initialize (IntPtr ifaceptr, IntPtr data)
		{
			AsyncInitableIface native_iface = (AsyncInitableIface) Marshal.PtrToStructure (ifaceptr, typeof (AsyncInitableIface));
			native_iface.init_async = iface.init_async;
			native_iface.init_finish = iface.init_finish;
			Marshal.StructureToPtr (native_iface, ifaceptr, false);
			GCHandle gch = (GCHandle) data;
			gch.Free ();
		}

		public AsyncInitableAdapter ()
		{
			InitHandler = new GLib.GInterfaceInitHandler (Initialize);
		}

		AsyncInitableImplementor implementor;

		public AsyncInitableAdapter (AsyncInitableImplementor implementor)
		{
			if (implementor == null)
				throw new ArgumentNullException (nameof (implementor));
			this.implementor = implementor;
		}

		public AsyncInitableAdapter (IntPtr handle)
		{
			this.handle = handle;
		}

		[DllImport("libgio-2.0-0.dll")]
		static extern IntPtr g_async_initable_get_type();

		private static GLib.GType _gtype = new GLib.GType (g_async_initable_get_type ());

		public override GLib.GType GType {
			get {
				return _gtype;
			}
		}

		IntPtr handle;
		public override IntPtr Handle {
			get {
				if (handle != IntPtr.Zero)
					return handle;
				return implementor == null ? IntPtr.Zero : implementor.Handle;
			}
		}

		public static AsyncInitable GetObject (IntPtr handle, bool owned)
		{
			GLib.Object obj = GLib.Object.GetObject (handle, owned);
			return GetObject (obj);
		}

		public static AsyncInitable GetObject (GLib.Object obj)
		{
			if (obj == null)
				return null;
			else if (obj is AsyncInitableImplementor)
				return new AsyncInitableAdapter (obj as AsyncInitableImplementor);
			else if (obj as AsyncInitable == null)
				return new AsyncInitableAdapter (obj.Handle);
			else
				return obj as AsyncInitable;
		}

		public AsyncInitableImplementor Implementor {
			get {
				return implementor;
			}
		}

		[DllImport("libgio-2.0-0.dll")]
		static extern bool g_async_initable_init_finish(IntPtr raw, IntPtr res, out IntPtr error);

		public bool InitFinish(GLib.AsyncResult res) {
			IntPtr error = IntPtr.Zero;
			bool raw_ret = g_async_initable_init_finish(Handle, res == null ? IntPtr.Zero : res.Handle, out error);
			bool ret = raw_ret;
			if (error != IntPtr.Zero) throw new GLib.GException (error);
			return ret;
		}

		[DllImport("libgio-2.0-0.dll")]
		static extern void g_async_initable_init_async(IntPtr raw, int io_priority, IntPtr cancellable, GLibSharp.AsyncReadyCallbackNative cb, IntPtr user_data);

		public void InitAsync(int io_priority, GLib.Cancellable cancellable, GLib.AsyncReadyCallback cb) {
			GLibSharp.AsyncReadyCallbackWrapper cb_wrapper = new GLibSharp.AsyncReadyCallbackWrapper (cb);
			cb_wrapper.PersistUntilCalled ();
			g_async_initable_init_async(Handle, io_priority, cancellable == null ? IntPtr.Zero : cancellable.Handle, cb_wrapper.NativeDelegate, IntPtr.Zero);
		}

#endregion
	}
}
