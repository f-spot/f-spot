using System;
using System.Reflection;

public static class GObjectIntPtrCtorVerifier
{
    public static void Main (string [] args)
    {
        foreach (var path in args) {
            Verify (path);
        }
    }

    private static void Verify (string path)
    {
        foreach (var type in Assembly.LoadFrom (path).GetTypes ()) {
            if (!type.IsSubclassOf (typeof (GLib.Object))) {
                continue;
            }

            bool safe = false;

            foreach (var ctor in type.GetConstructors (BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)) {
                var args = ctor.GetParameters ();
                if ((safe = (ctor.Attributes & (MethodAttributes.Public |
                    MethodAttributes.Family)) != 0 &&
                    args != null &&
                    args.Length == 1 &&
                    args[0].ParameterType == typeof (IntPtr))) {
                    break;
                }
            }

            if (!safe) {
                Console.WriteLine (type);
            }
        }
    }
}

