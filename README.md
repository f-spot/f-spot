F-Spot Photo Manager
http://f-spot.org/

Chat:
* [![Join the chat at https://gitter.im/mono/f-spot](https://badges.gitter.im/Join%20Chat.svg)](https://gitter.im/mono/f-spot?utm_source=badge&utm_medium=badge&utm_campaign=pr-badge&utm_content=badge)
* Also on #f-spot on gimpnet

Build Status:

| Branch | Status |
|--------|--------|
| Master |[![Build Status](https://travis-ci.org/mono/f-spot.svg?branch=master)](https://travis-ci.org/mono/f-spot)|

Requirements:

	- GNOME development libraries 2.4 or later,
	  http://www.gnome.org

	- Mono 3.8.2 or later, http://www.go-mono.net

	- gtk-sharp 2.12.2 or later, http://www.go-mono.net

	- Sqlite 2.8.6 or later

	- liblcms 2 or later, http://www.littlecms.com/

	- hicolor-icon-theme 0.10 or later, http://icon-theme.freedesktop.org/wiki/HicolorTheme

	- taglib-sharp 2.0.3.7 or later, https://github.com/mono/taglib-sharp

	- dbus-sharp 0.8 or later, https://github.com/mono/dbus-sharp

	- dbus-sharp-glib 0.6 or later, https://github.com/mono/dbus-sharp-glib

        - Nunit 2.6.4 if you want to run the unit tests, https://github.com/nunit/nunitv2/releases

To compile, just go through the normal autogen/configure stuff and
then make install.

To launch F-Spot, run $(prefix)/bin/f-spot.


With MonoDevelop:
  If you want to use MonoDevelop to build and run F-Spot here are notes about that process.

	There are a few steps you have to run before you can open MonoDevelop:
		1.  ./autogen.sh (on ubuntu you have to do ./autogen.sh)
		2.  cd build; make
		3.  cd lib/libfspot; make
		4.  sudo make install (this will install the libfspot.so files)
	- OR -
		1. ./prep_linux_build.sh prefix={some/path}
			I like to do ~/staging

	This will build a couple tools in ./build that are needed to build the projects
	in ./lib.

	Once these two directories are built you can now open monodevelop and build
	and run f-spot from there.
