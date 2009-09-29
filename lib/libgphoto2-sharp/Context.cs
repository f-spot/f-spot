/*
 * Context.cs
 *
 * Author(s):
 *	Stephane Delcroix <stephan @delcroix.org>
 *	Ewen Cheslack-Postava <echeslack@gmail.com>
 *	Larry Ewing <lewing@novell.com>
 *
 * Copyright (c) 2005-2009 Novell, Inc.
 *
 * This is open source software. See COPYING for details.
 */
using System;
using System.Runtime.InteropServices;

namespace GPhoto2
{
	public delegate void IdleFunc ();
	public delegate void ErrorFunc (string error);
	public delegate void StatusFunc (string status);
	public delegate void MessageFunc (string message);
	public delegate ContextFeedback QuestionFunc (string message);
	public delegate ContextFeedback CancelFunc (string message);
	public delegate uint ProgressStartFunc (float target, string message);
	public delegate void ProgressUpdateFunc (uint id, float progress);
	public delegate void ProgressStopFunc (uint id);

	public class Context : GPObject
	{
		[DllImport ("libgphoto2.so")]
		internal static extern IntPtr gp_context_new ();

		[DllImport ("libgphoto2glue.so")]
		internal static extern IntPtr gp_glue_context_new (HandleRef context);

		[DllImport ("libgphoto2.so")]
		internal static extern void gp_context_unref (HandleRef context);
		
		HandleRef gluecontext;
		public Context ()
		{
			this.handle = new HandleRef (this, gp_context_new ());
			gluecontext = new HandleRef (this, gp_glue_context_new (Handle));
		}
		

		[DllImport ("libgphoto2glue.so")]
		internal static extern void gp_glue_context_free (HandleRef context);

		protected override void Cleanup ()
		{
			gp_glue_context_free (gluecontext);
			gp_context_unref (handle);
		}

#region callbacks
		[DllImport ("libgphoto2.so")]
		internal static extern void gp_context_set_idle_func (HandleRef context, IdleFuncNative func, IntPtr data);

		IdleFuncWrapper idle_func_wrapper;
		public void SetIdleFunc (IdleFunc func)
		{
			idle_func_wrapper = new IdleFuncWrapper (func);
			IntPtr data = IntPtr.Zero;
			if (func != null)
				data = (IntPtr) GCHandle.Alloc (idle_func_wrapper);
			gp_context_set_idle_func (Handle, idle_func_wrapper.NativeDelegate, data);
		}

		[DllImport ("libgphoto2glue.so")]
		internal static extern void gp_glue_context_set_progress_funcs (HandleRef context, ProgressStartFuncNative start_func, ProgressUpdateFuncNative update_func, ProgressStopFuncNative stop_func, IntPtr data);

		ProgressFuncsWrapper progress_funcs_wrapper;
		public void SetProgressFuncs (ProgressStartFunc start_func, ProgressUpdateFunc update_func, ProgressStopFunc stop_func)
		{
			progress_funcs_wrapper = new ProgressFuncsWrapper (start_func, update_func, stop_func);
			IntPtr data = IntPtr.Zero;
			if (start_func != null || update_func != null || stop_func != null)
				data = (IntPtr) GCHandle.Alloc (progress_funcs_wrapper);
			gp_glue_context_set_progress_funcs (gluecontext,
							    progress_funcs_wrapper.NativeStartDelegate,
							    progress_funcs_wrapper.NativeUpdateDelegate,
							    progress_funcs_wrapper.NativeStopDelegate,
							    data);
		}

		[DllImport ("libgphoto2glue.so")]
		internal static extern void gp_glue_context_set_error_func (HandleRef context, ErrorFuncNative func, IntPtr data);

		ErrorFuncWrapper error_func_wrapper;
		public void SetErrorFunc (ErrorFunc func)
		{
			error_func_wrapper = new ErrorFuncWrapper (func);
			IntPtr data = IntPtr.Zero;
			if (func != null)
				data = (IntPtr) GCHandle.Alloc (error_func_wrapper);
			gp_glue_context_set_error_func (gluecontext, error_func_wrapper.NativeDelegate, data);
		}

		[DllImport ("libgphoto2glue.so")]
		internal static extern void gp_glue_context_set_status_func (HandleRef context, StatusFuncNative func, IntPtr data);

		StatusFuncWrapper status_func_wrapper;
		public void SetStatusFunc (StatusFunc func)
		{
			status_func_wrapper = new StatusFuncWrapper (func);
			IntPtr data = IntPtr.Zero;
			if (func != null)
				data = (IntPtr) GCHandle.Alloc (status_func_wrapper);
			gp_glue_context_set_status_func (gluecontext, status_func_wrapper.NativeDelegate, data);
		}

		[DllImport ("libgphoto2.so")]
		internal static extern void gp_context_set_message_func (HandleRef context, MessageFuncNative func, IntPtr data);

		MessageFuncWrapper message_func_wrapper;
		public void SetMessageFunc (MessageFunc func)
		{
			message_func_wrapper = new MessageFuncWrapper (func);
			IntPtr data = IntPtr.Zero;
			if (func != null)
				data = (IntPtr) GCHandle.Alloc (message_func_wrapper);
			gp_context_set_message_func (gluecontext, message_func_wrapper.NativeDelegate, data);
		}

#endregion

#region wrappers for callbacks
		internal delegate void IdleFuncNative (HandleRef context, IntPtr data);
		class IdleFuncWrapper
		{
			public void NativeCallback (HandleRef context, IntPtr data)
			{
				managed ();
			}

			public IdleFuncNative NativeDelegate;
			IdleFunc managed;

			public IdleFuncWrapper (IdleFunc managed)
			{
				this.managed = managed;
				if (managed != null)
					NativeDelegate = NativeCallback;
			}
		}

		internal delegate uint ProgressStartFuncNative (HandleRef context, float target, string message, IntPtr data);
		internal delegate void ProgressUpdateFuncNative (HandleRef context, uint id, float current, IntPtr data);
		internal delegate void ProgressStopFuncNative (HandleRef context, uint id, IntPtr data);
		class ProgressFuncsWrapper
		{
			public void NativeUpdateCallback (HandleRef context, uint id, float current, IntPtr data)
			{
				managed_update (id, current);
			}
			public uint NativeStartCallback (HandleRef context, float target, string message, IntPtr data)
			{
				return managed_start (target, message);
			}


			public void NativeStopCallback (HandleRef context, uint id, IntPtr data)
			{
				managed_stop (id);
			}

			public ProgressStartFuncNative NativeStartDelegate;
			public ProgressUpdateFuncNative NativeUpdateDelegate;
			public ProgressStopFuncNative NativeStopDelegate;

			ProgressStartFunc managed_start;
			ProgressUpdateFunc managed_update;
			ProgressStopFunc managed_stop;

			public ProgressFuncsWrapper (ProgressStartFunc start, ProgressUpdateFunc update, ProgressStopFunc stop)
			{
				managed_start = start;
				if (managed_start != null)
					NativeStartDelegate = new ProgressStartFuncNative (NativeStartCallback);

				managed_update = update;
				if (managed_update != null)
					NativeUpdateDelegate = new ProgressUpdateFuncNative (NativeUpdateCallback);

				managed_stop = stop;
				if (managed_stop != null)
					NativeStopDelegate = new ProgressStopFuncNative (NativeStopCallback);
			}
		}

		internal delegate void ErrorFuncNative (HandleRef context, string error, IntPtr data);
		class ErrorFuncWrapper
		{
			public void NativeCallback (HandleRef context, string error, IntPtr data)
			{
				managed (error);
			}

			public ErrorFuncNative NativeDelegate;
			ErrorFunc managed;

			public ErrorFuncWrapper (ErrorFunc managed)
			{
				this.managed = managed;
				if (managed != null)
					NativeDelegate = NativeCallback;
			}
		}

		internal delegate void StatusFuncNative (HandleRef context, string status, IntPtr data);
		class StatusFuncWrapper
		{
			public void NativeCallback (HandleRef context, string status, IntPtr data)
			{
				managed (status);
			}

			public StatusFuncNative NativeDelegate;
			StatusFunc managed;

			public StatusFuncWrapper (StatusFunc managed)
			{
				this.managed = managed;
				if (managed != null)
					NativeDelegate = NativeCallback;
			}
		}

		internal delegate void MessageFuncNative (HandleRef context, string message, IntPtr data);
		class MessageFuncWrapper
		{
			public void NativeCallback (HandleRef context, string message, IntPtr data)
			{
				managed (message);
			}

			public MessageFuncNative NativeDelegate;
			MessageFunc managed;

			public MessageFuncWrapper (MessageFunc managed)
			{
				this.managed = managed;
				if (managed != null)
					NativeDelegate = NativeCallback;
			}
		}


#endregion
	}
}
