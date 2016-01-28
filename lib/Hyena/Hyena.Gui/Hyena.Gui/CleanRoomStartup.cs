//
// CleanRoomStartup.cs
//
// Author:
//   Aaron Bockover <abockover@novell.com>
//
// Copyright (C) 2006-2007 Novell, Inc.
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

namespace Hyena.Gui
{
    public static class CleanRoomStartup
    {
        public delegate void StartupInvocationHandler();

        public static void Startup(StartupInvocationHandler startup)
        {
            bool disable_clean_room = false;

            foreach(string arg in Environment.GetCommandLineArgs ()) {
                if(arg == "--disable-clean-room") {
                    disable_clean_room = true;
                    break;
                }
            }

            if(disable_clean_room) {
                startup();
                return;
            }

            try {
                startup();
            } catch(Exception e) {
                Console.WriteLine(e.Message);
                Console.WriteLine(e);

                Gtk.Application.Init();
                Hyena.Gui.Dialogs.ExceptionDialog dialog = new Hyena.Gui.Dialogs.ExceptionDialog(e);
                dialog.Run();
                dialog.Destroy();
                System.Environment.Exit(1);
            }
        }
    }
}
