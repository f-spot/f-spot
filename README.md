F-Spot Photo Manager
http://f-spot.org/

# Chat
* [![Join the chat at https://gitter.im/mono/f-spot](https://badges.gitter.im/Join%20Chat.svg)](https://gitter.im/mono/f-spot?utm_source=badge&utm_medium=badge&utm_campaign=pr-badge&utm_content=badge)
* Also on #f-spot on gimpnet

# Build Status

| Branch | Status |
|--------|--------|
| Master |[![Build Status](https://travis-ci.org/mono/f-spot.svg?branch=master)](https://travis-ci.org/mono/f-spot)|

# Requirements

	- GNOME development libraries 2.4 or later, http://www.gnome.org

	- Mono 3.8.2 or later, http://www.go-mono.net

	- gtk-sharp 2.12.2 or later, http://www.go-mono.net

	- Sqlite 2.8.6 or later

	- liblcms 2 or later, http://www.littlecms.com/

	- hicolor-icon-theme 0.10 or later, https://www.freedesktop.org/wiki/Software/icon-theme

	- adwaita-icon-theme 3.18.0 or later, https://download.gnome.org/sources/adwaita-icon-theme/3.13

	- taglib-sharp 2.0.3.7 or later, https://github.com/mono/taglib-sharp

	- dbus-sharp 0.8 or later, https://github.com/mono/dbus-sharp

	- dbus-sharp-glib 0.6 or later, https://github.com/mono/dbus-sharp-glib

	- NuGet 2.14, if you want to build and run unit tests

The following requirements are automatically installed by make via
NuGet if you enable tests.

	- NUnit 2.6.4

	- Moq 4.2

# Installing missing Certificates

On distributions like Fedora or Mageia, Mono installations come without root certificates installed, and those may not necessarily be synced from the local root certificates as a post installation step either.
So on a fresh install, you may need to use the `cert-sync` tool in order to sync your local root certificates into the Mono truststore

More details in the [Mono 3.12 Release Notes](http://www.mono-project.com/docs/about-mono/releases/3.12.0/#cert-sync)

To invoke the tool manually use

```bash
sudo cert-sync /path/to/ca-bundle.crt
```

On Debian systems, that’s

```bash
sudo cert-sync /etc/ssl/certs/ca-certificates.crt
```

and on Red Hat derivatives (Fedora, CentOS, Mageia, etc...) it’s

```bash
sudo cert-sync /etc/pki/tls/certs/ca-bundle.crt
```

Your distribution might use a different path, if it’s not derived from one of those.

# Build

To compile, just go through the normal `autogen/configure` stuff and
then `make install`.

# Launch

To launch F-Spot, run `$(prefix)/bin/f-spot`.

# With MonoDevelop

If you want to use MonoDevelop to build and run F-Spot here are notes about that process.

There are a few steps you have to run before you can open MonoDevelop:

	1.  ./autogen.sh (on ubuntu you have to do ./autogen.sh)
	2.  cd build; make
	3.  cd lib/libfspot; make
	4.  sudo make install (this will install the libfspot.so files)

OR

	1. ./prep_linux_build.sh prefix={some/path}

I like to do `~/staging`

This will build a couple tools in ./build that are needed to build the projects in ./lib.

Once these two directories are built you can now open monodevelop and build and run f-spot from there.
