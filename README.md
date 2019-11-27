# F-Spot Photo Manager
http://f-spot.org/

# WARNING
The code base is in heavy flux right now. It might be completely broken right now.
Known issues:
* Doesn't start
* AdjustTimeDialog is completely broken due to removing Gnome.DateTime
* Autotools stuff is likely doing the wrongs things due to heavy changes.

# Chat
[![Join the chat at https://gitter.im/mono/f-spot](https://badges.gitter.im/Join%20Chat.svg)](https://gitter.im/mono/f-spot?utm_source=badge&utm_medium=badge&utm_campaign=pr-badge&utm_content=badge)

# Build Status

| Branch | Status |
|--------|--------|
| Master (Linux) |[![Build Status](https://travis-ci.org/f-spot/f-spot.svg?branch=master)](https://travis-ci.org/f-spot/f-spot)|
| Master (Linux) |[![Actions Status](https://github.com/f-spot/f-spot/workflows/Ubuntu/badge.svg)](https://github.com/f-spot/f-spot/actions) |
| Master (Windows) | [![Build Status](https://dev.azure.com/decriptor-oss/f-spot/_apis/build/status/Windows?branchName=master)](https://dev.azure.com/decriptor-oss/f-spot/_build/latest?definitionId=3&branchName=master) |


# Requirements

	- GNOME development libraries 2.4 or later, http://www.gnome.org

	- Mono 6.0.0 or later, http://www.mono-project.com

	- gtk-sharp 2.12.2 or later, http://www.mono-project.com

	- Sqlite 2.8.6 or later

	- liblcms 2 or later, http://www.littlecms.com/

	- hicolor-icon-theme 0.10 or later, https://www.freedesktop.org/wiki/Software/icon-theme

	- adwaita-icon-theme 3.18.0 or later, https://download.gnome.org/sources/adwaita-icon-theme/3.13

	- NuGet, https://www.nuget.org/

The following requirements are automatically installed by make via
NuGet

	- TagLibSharp 2.2.0

	Tests

	- NUnit 3.12.0

	- Moq 4.13.1

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

If you want to use <IDE> to build and run F-Spot here are notes about that process.

There are a few steps you have to run before you can open MonoDevelop:

	1.  ./autogen.sh (on ubuntu you have to do ./autogen.sh)
	2.  cd build; make
	3.  cd lib/libfspot; make
	4.  sudo make install (this will install the libfspot.so files)

OR

	1. ./prep_linux_build.sh prefix={some/path}

I like to do `~/staging`

This will build a couple tools in ./build that are needed to build the projects in ./lib.

Once these two directories are built you can now open <IDE> and build and run f-spot from there.
