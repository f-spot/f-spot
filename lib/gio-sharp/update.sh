#!/bin/bash

set -e

git clone https://github.com/mono/gio-sharp.git
pushd gio-sharp

# use mcs instead of gmcs to compile
sed -i "s/AC_PATH_PROG(CSC, gmcs)/AC_PATH_PROG(CSC, mcs)/" configure.ac.in

# fix generator
sed -i "s/GLib.CDeclCallback/UnmanagedFunctionPointer (CallingConvention.Cdecl)/" generator/{CallbackGen.cs,Signal.cs,VirtualMethod.cs}

# modify FileEnumerator
cat >> gio/FileEnumerator.custom <<EOF
public override void Dispose ()
{
	if (!IsClosed) {
		Close (null);
	}
	base.Dispose ();
}
EOF

# modify FileInfo.cs
cat >> gio/FileInfo.custom <<EOF
~FileInfo ()
{
	Dispose ();
}
EOF

./autogen-2.22.sh
make

pushd gio
find -name "*.cs" -exec cp "{}" "../../{}" \;
popd

popd
rm -rf gio-sharp

# remove signing key from AssemblyInfo
sed -i "/gtk-sharp.snk/d" AssemblyInfo.cs
