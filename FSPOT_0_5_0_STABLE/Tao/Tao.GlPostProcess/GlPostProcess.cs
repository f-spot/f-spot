//  -*- Mode: csharp; indent-tabs-mode: nil; tab-width: 50; c-basic-offset: 2 -*-
//  Tao.GlPostProcess.cs
//
//  This file is part of Tao.
//
//  Copyright (C) 2004  Vladimir Vukicevic  <vladimir@pobox.com>
//
//  This file is licensed under the MIT/X11 License, as outlined
//  in the License.txt file at the top level of this distribution.
//


using System;
using System.IO;
using System.Collections;
using System.Reflection;
using System.Reflection.Emit;
using System.Runtime.InteropServices;
using System.Security;
using System.Security.Permissions;

namespace Tao
{
  /// <summary>
  ///
  /// </summary>
  public class GlPostProcess
  {
    /// <summary>
    /// .config files are used to map this in mono
    /// </summary>
    public const string GL_NATIVE_LIBRARY = "opengl32.dll";

    static Hashtable field_hash;

    /// <summary>
    /// 
    /// </summary>
    /// <param name="args"></param>
    public static void Main (string [] args) {
      if (args.Length < 4 || args.Length > 5) {
        Console.WriteLine ("Usage: GlPostProcess.exe in.dll out.dll Tao.OpenGl.snk Tao.OpenGl.Gl [Tao.OpenGl.ContextGl]");
        return;
      }

      string inName = args[0];
      string outName = args[1];
      string typeName = args[3];
      string snkFile = null;
      if (args[2] != "")
        snkFile = args[2];
      string instanceTypeName = null;

      if (args.Length == 5)
        instanceTypeName = args[4];

      string outDir = System.IO.Path.GetDirectoryName(outName);
      string outDll = System.IO.Path.GetFileName(outName);
      string outNameBase = System.IO.Path.GetFileNameWithoutExtension(outName);

      // The MS runtime doesn't support a bunch of queries on
      // dynamic modules, so we have to track this stuff ourselves
      field_hash = new Hashtable();

      // load the input DLL as an Assembly
      Assembly inputAssembly = Assembly.LoadFrom(inName);

      // the calli instructions are unverifiable
      PermissionSet reqPermSet = new PermissionSet(PermissionState.None);
      reqPermSet.AddPermission(new SecurityPermission(SecurityPermissionFlag.SkipVerification));

      AssemblyName outAsName = new AssemblyName();
      outAsName.Name = outNameBase;
      if (snkFile != null)
        outAsName.KeyPair = new StrongNameKeyPair(File.Open(snkFile, FileMode.Open, FileAccess.Read));

      // create a dynamic assembly
      AssemblyBuilder abuilder = AppDomain.CurrentDomain.DefineDynamicAssembly
        (outAsName, AssemblyBuilderAccess.Save, outDir != null && outDir.Length > 0 ? outDir : null,
        reqPermSet, null, null);
      abuilder.SetCustomAttribute (GetCLSCompliantCAB (true));

      // go through and copy over custom attributes
      //object [] assemblyAttrs = inputAssembly.GetCustomAttributes (false);
      // copy over references
      //AssemblyName [] assemblyRefs = inputAssembly.GetReferencedAssemblies ();

      // create a dynamic module
      ModuleBuilder mbuilder = abuilder.DefineDynamicModule(outAsName.Name, outDll);
      mbuilder.SetCustomAttribute (GetCLSCompliantCAB (true));
      mbuilder.SetCustomAttribute (GetUnverifiableCodeCAB ());

      ProcessType (mbuilder, inputAssembly, typeName, false);
      if (instanceTypeName != null)
        ProcessType (mbuilder, inputAssembly, instanceTypeName, false);

      mbuilder.CreateGlobalFunctions();
      abuilder.Save(outDll);
    }

    /// <summary></summary>
    /// <returns></returns>

    // see below for win32 hack
    static FieldInfo win32SigField;

    /// <summary>
    /// takes a mbuilder for output, the input assembly, the name of the type,
    /// and a flag indicating whether it's an instance type or not (i.e. whether
    /// the members should be static or not.
    ///
    /// It then munges mercilessly.
    /// </summary>
    /// <param name="mbuilder"></param>
    /// <param name="inputAssembly"></param>
    /// <param name="typeName"></param>
    /// <param name="isInstanceClass"></param>
    public static void ProcessType (ModuleBuilder mbuilder,
                                    Assembly inputAssembly,
                                    string typeName,
                                    bool isInstanceClass)
    {
      TypeBuilder glbuilder = mbuilder.DefineType(typeName,
        TypeAttributes.Public |
        TypeAttributes.Class |
        TypeAttributes.Sealed);
      glbuilder.SetCustomAttribute (GetCLSCompliantCAB(true));

      Type gltype = inputAssembly.GetType(typeName);
      MemberInfo [] glMembers = gltype.GetMembers(BindingFlags.Instance |
        BindingFlags.Static |
        BindingFlags.Public |
        BindingFlags.NonPublic |
        BindingFlags.DeclaredOnly);

      // just something to help us print some status..
      int methodCount = 0;
      Console.Write ("Processing {0}...", typeName);

      foreach (MemberInfo qi in glMembers) {
        // Fields
        FieldInfo fi = qi as FieldInfo;
        if (fi != null) {
          // Console.WriteLine ("FIELD: " + fi.Name);
          FieldBuilder fb = glbuilder.DefineField (fi.Name, fi.FieldType, fi.Attributes);
          // only set constants in the non-instance class
          if (fi.FieldType != typeof(System.IntPtr) && !isInstanceClass) {
            fb.SetConstant (fi.GetValue (gltype));
          } else {
            object [] extattrs = fi.GetCustomAttributes (typeof(OpenGl.OpenGLExtensionImport), false);
            if (extattrs.Length > 0) {
              OpenGl.OpenGLExtensionImport ogl = extattrs[0] as OpenGl.OpenGLExtensionImport;
              if (ogl == null)
                throw new InvalidProgramException ("Thought we had an attr, guess we were wrong!");
              fb.SetCustomAttribute (CreateGLExtCAB (ogl.ExtensionName, ogl.EntryPoint));
            }

            // this is a slot to hold an extension addr,
            // so we save it.  We have to do this because on
            // windows we can't call GetField on a dynamic type.
            // This is probably faster anyway.
            field_hash[fi.Name] = fb;
          }
          continue;
        }

        // Methods
        MethodInfo mi = qi as MethodInfo;
        if (mi != null) {
          bool is_ext;
          bool is_dll;
          bool is_cls_compliant;
          object [] extattrs = mi.GetCustomAttributes (typeof(OpenGl.OpenGLExtensionImport), false);
          object [] clsattrs = mi.GetCustomAttributes (typeof(CLSCompliantAttribute), false);

          is_ext = (extattrs.Length > 0);
          is_dll = (mi.Attributes & MethodAttributes.PinvokeImpl) != 0;

          if (clsattrs.Length > 0)
            is_cls_compliant = (clsattrs[0] as CLSCompliantAttribute).IsCompliant;
          else
            is_cls_compliant = true;

          ParameterInfo [] parms = mi.GetParameters();
          Type [] methodSig = new Type [parms.Length];
          ParameterAttributes [] methodParams = new ParameterAttributes [parms.Length];
          for (int i = 0; i < parms.Length; i++) {
            methodSig[i] = parms[i].ParameterType;
            methodParams[i] = parms[i].Attributes;
          }

          // Console.WriteLine ("Method: {0} is_dll: {1}", mi.Name, is_dll);

          if (is_dll) {
            // this is a normal DLL import'd method
            // Console.WriteLine ("DLL import method: " + mi.Name);
            MethodBuilder mb = glbuilder.DefinePInvokeMethod (mi.Name, GL_NATIVE_LIBRARY, mi.Name,
              mi.Attributes,
              CallingConventions.Standard,
              mi.ReturnType, methodSig,
              CallingConvention.Winapi,
              CharSet.Ansi);
            mb.SetImplementationFlags(mb.GetMethodImplementationFlags() |
              MethodImplAttributes.PreserveSig);

            // Set In/Out/etc. back
            for (int i = 0; i < parms.Length; i++)
              mb.DefineParameter (i+1, methodParams[i], null);

            mb.SetCustomAttribute (GetSuppressUnmanagedCSCAB());
            if (is_cls_compliant)
              mb.SetCustomAttribute (GetCLSCompliantCAB(true));
            else
              mb.SetCustomAttribute (GetCLSCompliantCAB(false));
          } else if (is_ext) {
            // this is an OpenGLExtensionImport method
            OpenGl.OpenGLExtensionImport ogl = extattrs[0] as OpenGl.OpenGLExtensionImport;
            if (ogl == null)
              throw new InvalidProgramException ("Thought we had an OpenGLExtensionImport, guess not?");

            // Console.WriteLine ("OpenGL Extension method: " + mi.Name);
            MethodBuilder mb = glbuilder.DefineMethod (mi.Name, mi.Attributes, mi.ReturnType, methodSig);
            // Set In/Out/etc. back
            for (int i = 0; i < parms.Length; i++)
              mb.DefineParameter (i+1, methodParams[i], null);

            // put attributes
            mb.SetCustomAttribute (GetSuppressUnmanagedCSCAB());
            if (is_cls_compliant)
              mb.SetCustomAttribute (GetCLSCompliantCAB(true));
            else
              mb.SetCustomAttribute (GetCLSCompliantCAB(false));
            // now build the IL
            string fieldname = "ext__" + ogl.ExtensionName + "__" + ogl.EntryPoint;
            FieldInfo addrfield = field_hash[fieldname] as FieldInfo;

            // no workie on win32; the field_hash is probably faster anyway
            //        FieldInfo addrfield = glbuilder.GetField(fieldname,
            //                                                 BindingFlags.Instance |
            //                                                 BindingFlags.Static |
            //                                                 BindingFlags.Public |
            //                                                 BindingFlags.NonPublic |
            //                                                 BindingFlags.DeclaredOnly);

            ILGenerator ilg = mb.GetILGenerator();
            ArrayList locals = new ArrayList();
	    Type [] methodCalliSig = new Type[methodSig.Length];
            int thislocal;
            int numargs = methodSig.Length;
            int argoffset = 0;
            if (isInstanceClass)
              argoffset = 1;

            for (int arg = argoffset; arg < numargs; arg++) {
              EmitLdarg(ilg, arg);

              // we need to convert strings and string arrays to C
              // null-terminated strings.  Use StringToHGlobalAnsi
              // from Marshal.  We don't handle byref string arrays;
              // there are so few (any?) of them that we can just
              // return an IntPtr.
              if (methodSig[arg].IsArray &&
                  methodSig[arg].GetElementType() == typeof(string))
              {
                //Console.WriteLine ("String[] param: Method: {0} Param: {1} Type: {2}", mi.Name, arg - argoffset, methodSig[arg]);
                if (locals.Count == 0) locals.Add(ilg.DeclareLocal(typeof(int)));

                thislocal = locals.Count;
                locals.Add(ilg.DeclareLocal(typeof(IntPtr[])));

                // we have the source array on the stack; get its length,
                // and allocate a new IntPtr array
                ilg.Emit(OpCodes.Ldlen);
                ilg.Emit(OpCodes.Conv_I4);
                ilg.Emit(OpCodes.Newarr, typeof(IntPtr));
                EmitStloc(ilg, thislocal);

                // set our loop counter to 0;
                ilg.Emit(OpCodes.Ldc_I4_0);
                EmitStloc(ilg, 0);

                // declare our loop label; we'll branch
                // back to here after each conversion
                Label loop = ilg.DefineLabel();
                ilg.MarkLabel(loop);

                // put the address of the destination element onto the stack
                EmitLdloc(ilg, thislocal);
                EmitLdloc(ilg, 0);
                ilg.Emit(OpCodes.Ldelema, typeof(IntPtr));

                // put the source string on the stack
                EmitLdarg(ilg, arg);
                EmitLdloc(ilg, 0);
                ilg.Emit(OpCodes.Ldelem_Ref);

                // convert
                ilg.EmitCall(OpCodes.Call, GetStringMarshalMI(), null);

                // store the result into the address we put up above
                ilg.Emit(OpCodes.Stobj, typeof(IntPtr));

                // add 1 to loop counter
                EmitLdloc(ilg, 0);
                ilg.Emit(OpCodes.Ldc_I4_1);
                ilg.Emit(OpCodes.Add);
                ilg.Emit(OpCodes.Dup);
                EmitStloc(ilg, 0);

                // test if loop counter < array length, and branch back
                // to start of loop
                EmitLdarg(ilg, arg);
                ilg.Emit(OpCodes.Ldlen);
                ilg.Emit(OpCodes.Conv_I4);
                ilg.Emit(OpCodes.Blt, loop);

                // finally emit the location of the first element of the array
                EmitLdloc(ilg, thislocal);
                ilg.Emit(OpCodes.Ldc_I4_0);
                ilg.Emit(OpCodes.Ldelema, typeof(IntPtr));

		methodCalliSig[arg] = typeof(IntPtr);
              } else if (methodSig[arg].IsByRef &&
                         methodSig[arg].GetElementType() == typeof(string))
              {
                //Console.WriteLine ("String& param: Method: {0} Param: {1} Type: {2}", mi.Name, arg - argoffset, methodSig[arg]);
                //if (locals.Count == 0) locals.Add(ilg.DeclareLocal(typeof(int)));
                //thislocal = locals.Count;
                //locals.Add(ilg.DeclareLocal(typeof(IntPtr)));
		//methodCalliSig[arg] = typeof(IntPtr);
              } else if (methodSig[arg] == typeof(string)) {
                //Console.WriteLine ("String param: Method: {0} Param: {1} Type: {2}", mi.Name, arg - argoffset, methodSig[arg]);
                if (locals.Count == 0) locals.Add(ilg.DeclareLocal(typeof(int)));

                thislocal = locals.Count;
                locals.Add(ilg.DeclareLocal(typeof(IntPtr)));

                ilg.EmitCall(OpCodes.Call, GetStringMarshalMI(), null);
                ilg.Emit(OpCodes.Dup);
                EmitStloc(ilg, thislocal);

		methodCalliSig[arg] = typeof(IntPtr);
              } else {
		methodCalliSig[arg] = methodSig[arg];
	      }
            }

            if (isInstanceClass) {
              // load the instance field
              ilg.Emit(OpCodes.Ldarg_0);
              ilg.Emit(OpCodes.Ldfld, addrfield);
            } else {
              // just load the static field
              ilg.Emit(OpCodes.Ldsfld, addrfield);
            }

	    // emit Tailcall; have the return take place directly to our
	    // caller, but only if we have no marshal'd things to clean up
            if (locals.Count == 0) {
              ilg.Emit(OpCodes.Tailcall);
	      methodCalliSig = methodSig;
	    }

            // The .NET 1.1 runtime doesn't let us emit
            // CallingConvention.Winapi here, or anything else that
            // might mean "Use whatever the default platform calling
            // convention is", like p/invoke can have.
            //
            // However, Mono 1.1 /does/ let us emit Winapi/Default,
            // and both .NET 1.1 and Mono handle this as intended
            // (stdcall on .NET, cdecl on Mono/Linux).  By my reading
            // of ECMA-335 (CLI), 22.2.2, this should be allowed, and
            // I'm not sure why MS's impl doesn't allow it.  However,
            // the .NET System.Reflection.Emit leaves a lot to be
            // desired anyway.
            //
            // So, the issue is how to emit this.  On WIN32, we simply
            // create our own SignatureHelper, and munge the internal
            // signature byte.  Fun, eh?
            //
            // ... Except that it doesn't quite work that way. The runtime
            // gets confused by the calling convention.
            //
#if !WIN32
            // Mono?  Just emit normally.
            ilg.EmitCalli(OpCodes.Calli,
                          CallingConvention.Winapi,
                          mi.ReturnType, methodCalliSig);
#else

            // We're too smart for our own good.  We don't tell win32 how to do stack
            // cleanup, so it leaves things littering the stack.  So, we can't do this.
            // GRRRrrr.
#if true
            ilg.EmitCalli(OpCodes.Calli,
                          CallingConvention.StdCall,
                          mi.ReturnType, methodCalliSig);
#else
            // Win32?  Let the fun begin.
            if (win32SigField == null)
              win32SigField = typeof(SignatureHelper).GetField("m_signature",
                                                               BindingFlags.Instance |
                                                               BindingFlags.NonPublic);
            
            SignatureHelper sh = SignatureHelper.GetMethodSigHelper (mbuilder,
                                                                     CallingConvention.StdCall, // lie
                                                                     mi.ReturnType);
            // munge calling convention; the value in the first byte will be 0x2 for StdCall (1 minus
            // the CallingConvention enum value).  We set to 0.
            Array sigArr = win32SigField.GetValue(sh) as Array;
            sigArr.SetValue((byte) 0, 0);
            // then add the rest of the args.
            foreach (Type t in methodCalliSig)
              sh.AddArgument(t);
            ilg.Emit(OpCodes.Calli, sh);
#endif
#endif

            // clean up our string allocations, if any
            if (locals.Count > 0) {
              for (int i = 1; i < locals.Count; i++) {
                Type ltype = (locals[i] as LocalBuilder).LocalType;
                if (ltype.IsArray) {
                  Label looptest = ilg.DefineLabel();
                  Label loop = ilg.DefineLabel();

                  // counter = array.Length
                  EmitLdloc(ilg, i);
                  ilg.Emit(OpCodes.Ldlen);
                  ilg.Emit(OpCodes.Conv_I4);
                  EmitStloc(ilg, 0);

                  // goto looptest
                  ilg.Emit(OpCodes.Br, looptest);

                  ilg.MarkLabel(loop);

                  // free(array[counter])
                  EmitLdloc(ilg, i);
                  EmitLdloc(ilg, 0);
                  ilg.Emit(OpCodes.Ldelem_I4);
                  ilg.EmitCall(OpCodes.Call, GetFreeGlobalMI(), null);

                  ilg.MarkLabel(looptest);

                  // p = counter; counter -= 1;
                  EmitLdloc(ilg, 0);
                  ilg.Emit(OpCodes.Dup);
                  ilg.Emit(OpCodes.Ldc_I4_1);
                  ilg.Emit(OpCodes.Sub);
                  EmitStloc(ilg, 0);

                  // if (p > 0) goto loop
                  ilg.Emit(OpCodes.Ldc_I4_0);
                  ilg.Emit(OpCodes.Bgt, loop);
                } else {
                  // just a simple free, thankfully
                  EmitLdloc(ilg, i);
                  ilg.EmitCall(OpCodes.Call, GetFreeGlobalMI(), null);
                }
              }
            }

            ilg.Emit(OpCodes.Ret);
          } else {
            // this is a normal method
            // this shouldn't happen
            Console.WriteLine ();
            Console.WriteLine ("WARNING: Skipping non-DLL and non-Extension method " + mi.Name);
          }

          methodCount++;
          if (methodCount % 50 == 0)
            Console.Write(".");
          if (methodCount % 1000 == 0)
            Console.Write("[{0}]", methodCount);
        }
      }

      Console.WriteLine();

      glbuilder.CreateType();

      Console.WriteLine ("Type created.");
    }

    //
    // Helpers
    //

    static void EmitLdarg(ILGenerator ilg, int arg)
    {
      switch (arg) {
        case 0: ilg.Emit(OpCodes.Ldarg_0); break;
        case 1: ilg.Emit(OpCodes.Ldarg_1); break;
        case 2: ilg.Emit(OpCodes.Ldarg_2); break;
        case 3: ilg.Emit(OpCodes.Ldarg_3); break;
        default:ilg.Emit(OpCodes.Ldarg_S, arg); break;
      }
    }

    static void EmitStloc(ILGenerator ilg, int loc)
    {
      switch (loc) {
        case 0: ilg.Emit(OpCodes.Stloc_0); break;
        case 1: ilg.Emit(OpCodes.Stloc_1); break;
        case 2: ilg.Emit(OpCodes.Stloc_2); break;
        case 3: ilg.Emit(OpCodes.Stloc_3); break;
        default:ilg.Emit(OpCodes.Stloc_S, loc); break;
      }
    }
    static void EmitLdloc(ILGenerator ilg, int loc)
    {
      switch (loc) {
        case 0: ilg.Emit(OpCodes.Ldloc_0); break;
        case 1: ilg.Emit(OpCodes.Ldloc_1); break;
        case 2: ilg.Emit(OpCodes.Ldloc_2); break;
        case 3: ilg.Emit(OpCodes.Ldloc_3); break;
        default:ilg.Emit(OpCodes.Ldloc_S, loc); break;
      }
    }

    //
    // Custom attributes
    //
    static CustomAttributeBuilder clsCAB = null;
    static CustomAttributeBuilder GetCLSCompliantCAB (bool isCLScompliant)
    {
      if (clsCAB == null) {
        ConstructorInfo clsCI = typeof(CLSCompliantAttribute).GetConstructor(new Type [] { typeof(bool) });
        clsCAB = new CustomAttributeBuilder (clsCI, new object [] { isCLScompliant });
      }
      return clsCAB;
    }

    static CustomAttributeBuilder sumcsCAB = null;
    static CustomAttributeBuilder GetSuppressUnmanagedCSCAB ()
    {
      if (sumcsCAB == null) {
        ConstructorInfo sumcsCI = typeof(SuppressUnmanagedCodeSecurityAttribute).GetConstructor(new Type [] {});
        sumcsCAB = new CustomAttributeBuilder(sumcsCI, new object [] {});
      }
      return sumcsCAB;
    }

    static CustomAttributeBuilder outCAB = null;
    static CustomAttributeBuilder GetOutCAB () {
      if (outCAB == null) {
        ConstructorInfo outCI = typeof(OutAttribute).GetConstructor(new Type [] {});
        outCAB = new CustomAttributeBuilder(outCI, new object [] {});
      }
      return outCAB;
    }

    static CustomAttributeBuilder uvfCAB = null;
    static CustomAttributeBuilder GetUnverifiableCodeCAB () {
      if (uvfCAB == null) {
        ConstructorInfo uvfCI = typeof(UnverifiableCodeAttribute).GetConstructor(new Type [] {});
        uvfCAB = new CustomAttributeBuilder(uvfCI, new object [] {});
      }
      return uvfCAB;
    }

    static CustomAttributeBuilder CreateGLExtCAB (string extname, string procname) {
      Type [] ctorParams = new Type [] { typeof(string), typeof(string) };
      ConstructorInfo classCtorInfo = typeof(OpenGl.OpenGLExtensionImport).GetConstructor (ctorParams);
      CustomAttributeBuilder cab = new CustomAttributeBuilder (classCtorInfo,
        new object [] { extname, procname } );
      return cab;
    }

    static MethodInfo stringmarshalMI = null;
    static MethodInfo GetStringMarshalMI () {
      if (stringmarshalMI == null) {
        stringmarshalMI = typeof(System.Runtime.InteropServices.Marshal).GetMethod("StringToHGlobalAnsi");
      }
      return stringmarshalMI;
    }

    static MethodInfo freeglobalMI = null;
    static MethodInfo GetFreeGlobalMI () {
      if (freeglobalMI == null) {
        freeglobalMI = typeof(System.Runtime.InteropServices.Marshal).GetMethod("FreeHGlobal");
      }
      return freeglobalMI;
    }
  }
}
