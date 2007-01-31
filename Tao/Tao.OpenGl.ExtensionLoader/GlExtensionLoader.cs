// -*- Mode: csharp; tab-width: 40; indent-tabs-mode: nil; c-basic-offset: 2 -*-
//
//  GlExtensionLoader
//
//  Copyright (c) 2004  Vladimir Vukicevic  <vladimir@pobox.com>
//
//  This file is part of Tao.
//
//  This library is licensed under the MIT/X11 license.
//  Please see the file MIT.X11 for more information.
//

using System;
using System.Collections;
using System.Runtime.InteropServices;
using System.Reflection;

namespace Tao.OpenGl {

  //
  // This attribute is used to decorate OpenGL extension entry points.
  // It specifies both the extension name (full name, with GL_ prefix)
  // as well as the library entry point that should be queried for a
  // a particular method.  The field it's applied to will receive the
  // address of the function, whereas the method is only used before
  // postprocessing to tie a particular method with a particular extension.
  //
  /// <summary>
  /// 
  /// </summary>
  [AttributeUsage(AttributeTargets.Field | AttributeTargets.Method)]
  public class OpenGLExtensionImport : Attribute {
    /// <summary>
    /// 
    /// </summary>
    public string ExtensionName;
    /// <summary>
    /// 
    /// </summary>
    public string EntryPoint;

    /// <summary>
    /// 
    /// </summary>
    /// <param name="ExtensionName"></param>
    /// <param name="EntryPoint"></param>
    public OpenGLExtensionImport (string ExtensionName, string EntryPoint) {
      this.ExtensionName = ExtensionName;
      this.EntryPoint = EntryPoint;
    }
  }

  //
  // The GlExtensionLoader singleton, available through GetInstance(),
  // is responsible for loading extensions.
  //
  /// <summary>
  /// 
  /// </summary>
  public class GlExtensionLoader {
    //
    // Data for a particular context; available extensions,
    // already-loaded extensions, etc.
    //

    internal class GlContextInfo {
      public Hashtable AvailableExtensions;
      public Hashtable LoadedExtensions;

      public GlContextInfo() {
        AvailableExtensions = new Hashtable();
        LoadedExtensions = new Hashtable();

        ParseAvailableExtensions();
      }

      public void ParseAvailableExtensions() {
        // assumes that the context is already made current
        IntPtr extstrptr = glGetString(0x00001f03); // GL_EXTENSIONS
        if (extstrptr == IntPtr.Zero)
          return;               // no extensions are available

        string extstr = Marshal.PtrToStringAnsi (extstrptr);

        string [] exts = extstr.Split(' ');
        foreach (string ext in exts) {
          AvailableExtensions[ext] = true;
        }

        IntPtr verstrptr = glGetString(0x1F02); // GL_VERSION
        if (verstrptr == IntPtr.Zero)
          return;               // this shoudn't happen
    
        string verstr = Marshal.PtrToStringAnsi (verstrptr).Trim(new char[] {' '});

        if( verstr.StartsWith("1.2") ) 
        {
          AvailableExtensions["GL_VERSION_1_2"] = true;
        }
        else if( verstr.StartsWith("1.3") ) 
        {
          AvailableExtensions["GL_VERSION_1_2"] = true;
          AvailableExtensions["GL_VERSION_1_3"] = true;
        }
        else if( verstr.StartsWith("1.4") ) 
        {
          AvailableExtensions["GL_VERSION_1_2"] = true;
          AvailableExtensions["GL_VERSION_1_3"] = true;
          AvailableExtensions["GL_VERSION_1_4"] = true;
        }
        else if( verstr.StartsWith("1.5") ) 
        {
          AvailableExtensions["GL_VERSION_1_2"] = true;
          AvailableExtensions["GL_VERSION_1_3"] = true;
          AvailableExtensions["GL_VERSION_1_4"] = true;
          AvailableExtensions["GL_VERSION_1_5"] = true;
        }
        else if( verstr.StartsWith("2") )
        {
          AvailableExtensions["GL_VERSION_1_2"] = true;
          AvailableExtensions["GL_VERSION_1_3"] = true;
          AvailableExtensions["GL_VERSION_1_4"] = true;
          AvailableExtensions["GL_VERSION_1_5"] = true;
          AvailableExtensions["GL_VERSION_2_0"] = true;
        }
      }
    }

    // key -> GlContextInfo
    // 0 is special key for the static context
    private static Hashtable ContextInfo;

    static GlExtensionLoader() { 
        ContextInfo = new Hashtable();
    }

    // we can't depend on any symbols from Tao.OpenGl.Gl

    // linux
    [DllImport("opengl32.dll", EntryPoint="glXGetProcAddress")]
    internal static extern IntPtr glxGetProcAddress(string s);

    // also linux, for our ARB-y friends
    [DllImport("opengl32.dll", EntryPoint="glXGetProcAddressARB")]
    internal static extern IntPtr glxGetProcAddressARB(string s);

    // windows
    [DllImport("opengl32.dll", EntryPoint="wglGetProcAddress")]
    internal static extern IntPtr wglGetProcAddress(string s);

    // osx gets complicated
    [DllImport("libdl.dylib", EntryPoint="NSIsSymbolNameDefined")]
    internal static extern bool NSIsSymbolNameDefined(string s);
    [DllImport("libdl.dylib", EntryPoint="NSLookupAndBindSymbol")]
    internal static extern IntPtr NSLookupAndBindSymbol(string s);
    [DllImport("libdl.dylib", EntryPoint="NSAddressOfSymbol")]
    internal static extern IntPtr NSAddressOfSymbol(IntPtr symbol);

    // we can't depend on Tao.OpenGl.Gl for this
    
    [DllImport("opengl32.dll")]
    internal static extern IntPtr glGetString(uint name);

    internal static IntPtr aglGetProcAddress(string s) {
      string fname = "_" + s;
      if (!NSIsSymbolNameDefined(fname))
        return IntPtr.Zero;

      IntPtr symbol = NSLookupAndBindSymbol(fname);
      if (symbol != IntPtr.Zero)
        symbol = NSAddressOfSymbol(symbol);

      return symbol;
    }

    internal static GlContextInfo GetContextInfo(object ctx) {
      // use "0" to mean no context
      if (ctx == null)
        ctx = 0;

      if (!ContextInfo.ContainsKey(ctx)) {
        ContextInfo[ctx] = new GlContextInfo();
      }

      return ContextInfo[ctx] as GlContextInfo;
    }

    //
    // the public entry point for a cross-platform GetProcAddress
    //
    enum GetProcAddressPlatform {
      Unknown,
      Windows,
      X11,
      X11_ARB,
      OSX
    };

    static GetProcAddressPlatform gpaPlatform = GetProcAddressPlatform.Unknown;

    /// <summary>
    /// 
    /// </summary>
    /// <param name="s"></param>
    /// <returns></returns>
    public static IntPtr GetProcAddress(string s) {
      if (gpaPlatform == GetProcAddressPlatform.Unknown) {
        IntPtr result = IntPtr.Zero;

        // WGL?
        try {
          result = wglGetProcAddress(s);
          gpaPlatform = GetProcAddressPlatform.Windows;
          return result;
        } catch (Exception) {
        }

        // AGL? (before X11, since GLX might exist on OSX)
        try {
          result = aglGetProcAddress(s);
          gpaPlatform = GetProcAddressPlatform.OSX;
          return result;
        } catch (Exception) {
        }

        // X11?
        try {
          result = glxGetProcAddress(s);
          gpaPlatform = GetProcAddressPlatform.X11;
          return result;
        } catch (Exception) {
        }

        // X11 ARB?
        try {
          result = glxGetProcAddressARB(s);
          gpaPlatform = GetProcAddressPlatform.X11_ARB;
          return result;
        } catch (Exception) {
        }

        // Ack!
        throw new NotSupportedException ("Can't figure out how to call GetProcAddress on this platform!");
      } else if (gpaPlatform == GetProcAddressPlatform.Windows) {
        return wglGetProcAddress(s);
      } else if (gpaPlatform == GetProcAddressPlatform.OSX) {
        return aglGetProcAddress(s);
      } else if (gpaPlatform == GetProcAddressPlatform.X11) {
        return glxGetProcAddress(s);
      } else if (gpaPlatform == GetProcAddressPlatform.X11_ARB) {
        return glxGetProcAddressARB(s);
      }

      throw new NotSupportedException ("Shouldn't get here..");
    }

    private GlExtensionLoader () {
    }

    /// <summary>
    /// Returns trueif the extension with the given name is supported
    /// in the global static context.
    /// </summary>
    /// <param name="extname">The extension name.</param>
    /// <returns></returns>
    public static bool IsExtensionSupported (string extname) {
      return IsExtensionSupported (null, extname);
    }

    /// <summary>
    /// Returns true if the extension with the given name is supported
    /// in the given context.
    /// </summary>
    /// <param name="contextGl">The context which to query.</param>
    /// <param name="extname">The extension name.</param>
    /// <returns></returns>
    public static bool IsExtensionSupported (object contextGl, string extname) {
      GlContextInfo gci = GetContextInfo(contextGl);
      if (gci.AvailableExtensions.ContainsKey (extname))
        return true;
      return false;
    }

    /// <summary>
    /// Attempt to load the extension with the specified name into the
    /// global static context.  Returns true on success.
    /// </summary>
    /// <param name="extname">The extension name.</param>
    /// <returns></returns>
    public static bool LoadExtension (string extname) {
      return LoadExtension (null, extname, false);
    }

    //
    // LoadExtension
    //
    // Attempt to load the extension with the specified name into the
    // given context, which must have already been made current.  The
    // object passed in ought to be an instance of
    // Tao.OpenGl.ContextGl, or null.
    //
    /// <summary>
    /// 
    /// </summary>
    /// <param name="contextGl"></param>
    /// <param name="extname"></param>
    /// <returns></returns>
    public static bool LoadExtension (object contextGl, string extname) {
      return LoadExtension (contextGl, extname, false);
    }

    //
    // LoadExtension
    //
    // Attempt to load the extension with the specified name into the
    // given context, which must have already been made current.  The
    // object passed in ought to be an instance of
    // Tao.OpenGl.ContextGl, or null. If forceLoad is set, attempt
    // to obtain function pointers even if the runtime claims that the
    // extension is not supported.
    //
    /// <summary>
    /// 
    /// </summary>
    /// <param name="contextGl"></param>
    /// <param name="extname"></param>
    /// <param name="forceLoad"></param>
    /// <returns></returns>
    public static bool LoadExtension (object contextGl, string extname, bool forceLoad) {
      GlContextInfo gci = GetContextInfo(contextGl);
      if (gci.LoadedExtensions.ContainsKey (extname)) {
        return (bool) gci.LoadedExtensions[extname];
      }

      if (!forceLoad && !gci.AvailableExtensions.ContainsKey (extname)) {
        return false;
      }

      // this will get us either the Tao.OpenGl.Gl or
      // Tao.OpenGl.ContextGl class
      Type glt;
      if (contextGl != null) {
        glt = contextGl.GetType();
      } else {
        glt = StaticGlType;
        if (glt == null) {
          Console.WriteLine ("GL type is null!");
        }
      }

      FieldInfo [] fis = glt.GetFields (BindingFlags.Public |
                                        BindingFlags.DeclaredOnly |
                                        BindingFlags.Static |
                                        BindingFlags.Instance);

      foreach (FieldInfo fi in fis) {
        object [] attrs = fi.GetCustomAttributes (typeof(OpenGLExtensionImport), false);
        if (attrs.Length == 0)
          continue;

        OpenGLExtensionImport oglext = attrs[0] as OpenGLExtensionImport;
        if (oglext.ExtensionName == extname) {
          // did we already load this somehow?
          if (((IntPtr) fi.GetValue(contextGl)) != IntPtr.Zero)
            continue;

          //Console.WriteLine ("Loading " + oglext.EntryPoint + " for " + extname);
          IntPtr procaddr = GetProcAddress (oglext.EntryPoint);
          if (procaddr == IntPtr.Zero) {
            Console.WriteLine ("OpenGL claimed that '{0}' was supported, but couldn't find '{1}' entry point",
                               extname, oglext.EntryPoint);
            // we crash if anyone tries to call this method, but that's ok
            continue;
          }

          fi.SetValue (contextGl, procaddr);
        }
      }

      gci.LoadedExtensions[extname] = true;
      return true;
    }

    //
    // LoadAllExtensions
    //

    /// <summary>
    /// 
    /// </summary>
    public static void LoadAllExtensions () {
      LoadAllExtensions (null);
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="contextGl"></param>
    public static void LoadAllExtensions (object contextGl) {
      GlContextInfo gci = GetContextInfo(contextGl);
      
      foreach (string ext in gci.AvailableExtensions.Keys)
        LoadExtension (contextGl, ext, false);
    }

    //
    // Find the Tao.OpenGl.Gl type
    //
    static Type mStaticGlType;
    static Type StaticGlType {
      get {
        if (mStaticGlType != null)
          return mStaticGlType;

        foreach (Assembly asm in AppDomain.CurrentDomain.GetAssemblies()) {
          mStaticGlType = asm.GetType("Tao.OpenGl.Gl");
          if (mStaticGlType != null)
            return mStaticGlType;
        }

        throw new InvalidProgramException("Can't find Tao.OpenGl.Gl type in any loaded assembly!");
      }
    }
  }
}
