using System;
using System.Runtime.InteropServices;

namespace LibGPhoto2
{
	[StructLayout(LayoutKind.Sequential)]
	internal unsafe struct _Context
	{
		[DllImport ("libgphoto2.so")]
		internal static extern _Context *gp_context_new ();
		
		[DllImport ("libgphoto2.so")]
		internal static extern void gp_context_ref   (_Context *context);
		
		[DllImport ("libgphoto2.so")]
		internal static extern void gp_context_unref (_Context *context);
	}
	
	public class Context : IDisposable
	{
		unsafe _Context *obj;
		
		public Context()
		{
			unsafe 
			{
				obj = _Context.gp_context_new();
			}
		}
		
		public void Dispose()
		{
			Dispose(true);
			GC.SuppressFinalize(this);
		}
		
		~Context()
		{
			Dispose(false);
		}
		
		protected virtual void Dispose (bool disposing)
		{
			unsafe
			{
				if (obj != null)
				{
					_Context.gp_context_unref(obj);
					obj = null;
				}
			}
		}
		
		unsafe internal _Context* UnsafeContext
		{
			get
			{
				return obj;
			}
		}
	}
}
