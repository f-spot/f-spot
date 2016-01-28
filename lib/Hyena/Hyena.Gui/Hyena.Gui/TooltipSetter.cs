//
// TooltipSetter.cs
//
// Author:
//   Aaron Bockover <abockover@novell.com>
//
// Copyright (C) 2008 Novell, Inc.
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

using Gtk;

namespace Hyena.Gui
{
    public static class TooltipSetter
    {
        private static Type host_type;
        private static MethodInfo host_set_tip_method;
        private static PropertyInfo tooltip_text_property;
        private static bool reflected;

        public static object CreateHost ()
        {
            if (tooltip_text_property != null) {
                return null;
            }

            Type type = reflected ? null : typeof (Widget);

            if (type != null) {
                tooltip_text_property = type.GetProperty ("TooltipText", BindingFlags.Instance | BindingFlags.Public);
                if (tooltip_text_property != null) {
                    reflected = true;
                    return null;
                }
            }

            if (host_set_tip_method == null && !reflected) {
                reflected = true;
                host_type = Type.GetType (String.Format ("Gtk.Tooltips, {0}", type.Assembly.FullName));
                if (type == null) {
                    return null;
                }

                host_set_tip_method = host_type.GetMethod ("SetTip", BindingFlags.Instance |
                    BindingFlags.Public | BindingFlags.InvokeMethod);
                if (host_set_tip_method == null) {
                    return null;
                }
            }

            return host_set_tip_method != null ? Activator.CreateInstance (host_type) : null;
        }

        public static void Set (object host, Widget widget, string textTip)
        {
            if (tooltip_text_property != null) {
                tooltip_text_property.SetValue (widget, textTip, null);
            } else if (host != null && host_set_tip_method != null) {
                host_set_tip_method.Invoke (host, new object [] { widget, textTip, null });
            } else {
                throw new ApplicationException ("You must call TooltipSetter.CreateHost before calling TooltipSetter.Set");
            }
        }
    }
}
