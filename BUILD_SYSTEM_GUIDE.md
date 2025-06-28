# F-Spot Extended Build System Guide

This comprehensive guide covers platform-specific build instructions, development environment setup, cross-compilation scenarios, CI/CD pipeline analysis, packaging and distribution, troubleshooting, and advanced build scenarios for the F-Spot photo management application.

## Table of Contents

1. [Build System Overview](#build-system-overview)
2. [Platform-Specific Build Instructions](#platform-specific-build-instructions)
3. [Development Environment Setup](#development-environment-setup)
4. [Cross-Compilation Scenarios](#cross-compilation-scenarios)
5. [CI/CD Pipeline Analysis](#cicd-pipeline-analysis)
6. [Packaging and Distribution](#packaging-and-distribution)
7. [Troubleshooting Common Build Issues](#troubleshooting-common-build-issues)
8. [Advanced Build Scenarios](#advanced-build-scenarios)
9. [Dependency Management](#dependency-management)

## Build System Overview

F-Spot employs a hybrid build system that supports both traditional autotools (for Linux/Unix) and modern MSBuild/.NET (for cross-platform development):

### Architecture
- **Primary Build System**: MSBuild with .NET Framework 4.7.2 target
- **Legacy Support**: Autotools for Linux package maintainers
- **Package Management**: Centralized NuGet package management
- **Target Framework**: .NET Framework 4.7.2 (net472)
- **Platform Detection**: Automatic platform detection via MSBuild properties

### Key Build Files
- `build.proj` - Main MSBuild entry point
- `F-Spot.sln` - Visual Studio solution file
- `Directory.Build.props` - Global MSBuild properties
- `Directory.Packages.props` - Centralized package version management
- `fspot.props` - Platform detection logic
- `configure.ac` - Autotools configuration
- `autogen.sh` - Autotools bootstrap script

## Platform-Specific Build Instructions

### Linux (Ubuntu/Debian)

#### Prerequisites Installation
```bash
# Essential build tools
sudo apt update
sudo apt install -y build-essential automake libtool intltool

# .NET/Mono runtime and development
sudo apt install -y mono-complete mono-devel nuget

# GTK# and GNOME development libraries
sudo apt install -y libgtk2.0-dev libglib2.0-dev liblcms2-dev libjpeg-dev
sudo apt install -y libgtk2.0-cil-dev libglib2.0-cil-dev
sudo apt install -y gtk-sharp2-gapi libgtk-sharp-beans2.0-cil-dev

# Icon themes and additional dependencies
sudo apt install -y adwaita-icon-theme hicolor-icon-theme
sudo apt install -y libsqlite3-dev pkg-config gettext
```

#### Build Methods

**Method 1: MSBuild (Recommended)**
```bash
# Clone the repository
git clone https://github.com/f-spot/f-spot.git
cd f-spot

# Restore NuGet packages
nuget restore F-Spot.sln
# or
dotnet restore F-Spot.sln

# Build the solution
msbuild build.proj -p:Configuration=Release
# or
dotnet build F-Spot.sln -c Release

# Run the application
msbuild build.proj -t:Run
```

**Method 2: Autotools (Traditional)**
```bash
# Quick setup using provided script
./prep_linux_build.sh --prefix=/usr/local

# Manual autotools build
./autogen.sh --prefix=/usr/local --enable-tests
make -j$(nproc)
sudo make install

# Run tests
make test
```

#### Distribution-Specific Notes

**Fedora/RHEL/CentOS:**
```bash
# Install dependencies
sudo dnf install -y automake libtool intltool mono-devel nuget
sudo dnf install -y gtk2-devel glib2-devel lcms2-devel libjpeg-devel
sudo dnf install -y gtk-sharp2-devel gtk-sharp2-gapi

# Certificate sync for HTTPS NuGet
sudo cert-sync /etc/pki/tls/certs/ca-bundle.crt
```

**Arch Linux:**
```bash
# Install dependencies
sudo pacman -S mono nuget gtk2 glib2 lcms2 libjpeg
sudo pacman -S gtk-sharp-2 glib-sharp

# Build using AUR helper or manual PKGBUILD
```

**openSUSE:**
```bash
# Install dependencies
sudo zypper install mono-devel nuget gtk2-devel glib2-devel
sudo zypper in gtk-sharp2 gtk-sharp2-gapi
```

### Windows

#### Prerequisites Installation

**Option 1: Chocolatey (Recommended)**
```powershell
# Install Chocolatey first, then:
choco install gtksharp
choco install nuget.commandline
choco install dotnetframework
```

**Option 2: Manual Installation**
1. Install [.NET Framework 4.7.2+](https://dotnet.microsoft.com/download/dotnet-framework)
2. Install [GTK# for .NET](http://www.mono-project.com/download/stable/#download-win)
3. Install [NuGet CLI](https://www.nuget.org/downloads)
4. Install [MSBuild](https://visualstudio.microsoft.com/downloads/#build-tools-for-visual-studio-2022)

#### Build Process
```powershell
# Clone repository
git clone https://github.com/f-spot/f-spot.git
cd f-spot

# Restore NuGet packages
nuget.exe restore .nuget\packages.config -PackagesDirectory packages
nuget.exe restore F-Spot.sln

# Build using MSBuild
msbuild build.proj -p:Configuration=Release -p:Platform=x86

# Alternative: Visual Studio
# Open F-Spot.sln in Visual Studio and build normally
```

#### Platform-Specific Considerations
- **Platform Target**: Forced to x86 on Windows due to GTK# limitations
- **Config Files**: Automatic copying of .config.in templates
- **GTK# Dependencies**: Must be installed system-wide

### macOS

#### Prerequisites Installation

**Using Homebrew:**
```bash
# Install Homebrew first, then:
brew install mono pkg-config autoconf automake libtool
brew install gtk+ glib cairo pango

# Install GTK# (may require manual compilation)
```

**Using MacPorts:**
```bash
sudo port install mono pkgconfig autoconf automake libtool
sudo port install gtk2 +quartz +x11
```

#### Build Process
```bash
# Clone repository
git clone https://github.com/f-spot/f-spot.git
cd f-spot

# Restore packages
nuget restore F-Spot.sln

# Build using MSBuild/Mono
msbuild build.proj -p:Configuration=Release

# Alternative: Traditional build
./autogen.sh --prefix=/usr/local
make -j$(sysctl -n hw.ncpu)
make install
```

#### macOS-Specific Challenges
- **GTK# Availability**: Limited packages available
- **Framework Support**: May require manual GTK# compilation
- **Code Signing**: May require developer certificates for distribution

## Development Environment Setup

### Visual Studio / Visual Studio Code

**Visual Studio (Windows/Mac):**
1. Install Visual Studio 2019/2022 with .NET Framework development
2. Install GTK# for .NET
3. Clone repository and open `F-Spot.sln`
4. Build → Build Solution

**Visual Studio Code (Cross-platform):**
```json
// .vscode/tasks.json
{
    "version": "2.0.0",
    "tasks": [
        {
            "label": "build",
            "command": "msbuild",
            "args": ["build.proj", "-p:Configuration=Debug"],
            "group": "build",
            "presentation": {
                "echo": true,
                "reveal": "always",
                "focus": false,
                "panel": "shared"
            }
        },
        {
            "label": "test",
            "command": "msbuild",
            "args": ["build.proj", "-t:Test"],
            "group": "test"
        }
    ]
}
```

### MonoDevelop (Linux)

1. Install MonoDevelop: `sudo apt install monodevelop`
2. Run initial autotools setup:
   ```bash
   ./autogen.sh
   cd build && make && cd ../lib && make
   cd libfspot && sudo make install && cd ../..
   ```
3. Open `F-Spot.sln` in MonoDevelop

### Development Dependencies

**Essential Development Tools:**
```bash
# Code analysis and formatting
dotnet tool install -g dotnet-format
dotnet tool install -g dotnet-outdated-tool

# Additional debugging tools
sudo apt install -y gdb mono-utils
```

## Cross-Compilation Scenarios

### Building for Different Architectures

**x64 to x86 (Windows):**
```powershell
msbuild build.proj -p:Platform=x86 -p:Configuration=Release
```

**Cross-compilation Challenges:**
1. **GTK# Architecture Matching**: GTK# must match target architecture
2. **Native Dependencies**: libfspot.so must be compiled for target architecture
3. **Platform Detection**: MSBuild automatically detects platform but can be overridden

### Docker-Based Cross-Compilation

**Linux Multi-Architecture Container:**
```dockerfile
# Dockerfile.multiarch
FROM mcr.microsoft.com/dotnet/framework/sdk:4.8-windowsservercore-ltsc2019 AS windows-build
COPY . /src
WORKDIR /src
RUN msbuild build.proj -p:Configuration=Release

FROM mono:latest AS linux-build
RUN apt-get update && apt-get install -y \
    build-essential automake libtool intltool \
    libgtk2.0-dev libglib2.0-dev liblcms2-dev
COPY . /src
WORKDIR /src
RUN ./autogen.sh && make
```

## CI/CD Pipeline Analysis

### Azure Pipelines Configuration

The project uses Azure Pipelines with a matrix build strategy across three platforms:

#### Pipeline Structure
```yaml
# azure-pipelines.yml
name: $(Build.SourceBranch)-$(Build.SourceVersion)-$(Rev:r)

variables:
- template: .build/automation/variables.yml

stages:
  - template: .build/automation/stages/validate.yml
```

#### Platform-Specific Jobs

**Linux Job (`.build/automation/jobs/linux.yml`):**
- **Base Image**: ubuntu-latest
- **Build Method**: Autotools
- **Dependencies**: Installed via apt
- **Test Execution**: `make test`

**Windows Job (`.build/automation/jobs/windows.yml`):**
- **Base Image**: windows-latest
- **Build Method**: MSBuild
- **Dependencies**: Chocolatey (gtksharp), NuGet
- **Platform**: x86 (forced)
- **Test Results**: Published via NUnit format

**macOS Job (`.build/automation/jobs/mac.yml`):**
- **Base Image**: macOS-latest
- **Build Method**: MSBuild
- **Dependencies**: NuGet packages only
- **Challenges**: No GTK# installation step

#### Build Variables
```yaml
# .build/automation/variables.yml
variables:
- name: DefaultBuildConfiguration
  value: Release
- name: SolutionName
  value: build.proj
- name: WindowsImage
  value: windows-latest
- name: MacImage
  value: macOS-latest
- name: LinuxImage
  value: ubuntu-latest
```

### Extending the Pipeline

**Adding Additional Platforms:**
```yaml
# .build/automation/jobs/alpine.yml
jobs:
  - job: Alpine
    displayName: Alpine Linux
    pool:
      vmImage: ubuntu-latest
    container: alpine:latest
    steps:
      - script: |
          apk add --no-cache mono-dev gtk+2.0-dev build-base
          msbuild build.proj
```

**Adding Code Quality Checks:**
```yaml
# Additional pipeline stage
- stage: quality
  displayName: 'Code Quality'
  jobs:
    - job: analysis
      steps:
        - task: SonarCloudPrepare@1
        - task: MSBuild@1
        - task: SonarCloudAnalyze@1
```

## Packaging and Distribution

### Linux Package Creation

**Debian/Ubuntu Package:**
```bash
# debian/control
Source: f-spot
Section: graphics
Priority: optional
Maintainer: F-Spot Team <maintainer@f-spot.org>
Build-Depends: debhelper (>= 12),
               mono-devel,
               libgtk2.0-dev,
               libglib2.0-cil-dev,
               liblcms2-dev

Package: f-spot
Architecture: any
Depends: ${shlibs:Depends}, ${misc:Depends},
         mono-runtime,
         libgtk2.0-cil,
         libglib2.0-cil
Description: Personal photo management application
 F-Spot is a personal photo management application for the GNOME desktop.
```

**RPM Spec File:**
```spec
# f-spot.spec
Name:           f-spot
Version:        0.9.0
Release:        1%{?dist}
Summary:        Personal photo management application
License:        GPLv2+
URL:            https://github.com/f-spot/f-spot
Source0:        %{name}-%{version}.tar.bz2

BuildRequires:  mono-devel
BuildRequires:  gtk2-devel
BuildRequires:  glib2-devel
BuildRequires:  lcms2-devel
BuildRequires:  autotools

Requires:       mono-runtime
Requires:       gtk-sharp2

%description
F-Spot is a personal photo management application for the GNOME desktop.

%prep
%setup -q

%build
./autogen.sh --prefix=%{_prefix}
make %{?_smp_mflags}

%install
make install DESTDIR=%{buildroot}

%files
%{_bindir}/f-spot
%{_libdir}/f-spot/
%{_datadir}/f-spot/
```

### Windows Distribution

**Installer Creation (WiX):**
```xml
<!-- Product.wxs -->
<Wix xmlns="http://schemas.microsoft.com/wix/2006/wi">
  <Product Id="*" Name="F-Spot" Language="1033" Version="0.9.0"
           Manufacturer="F-Spot Project" UpgradeCode="...">
    
    <Package Description="F-Spot Photo Manager"
             Comments="Personal photo management for Windows"
             Compressed="yes" />
    
    <Directory Id="TARGETDIR" Name="SourceDir">
      <Directory Id="ProgramFilesFolder">
        <Directory Id="INSTALLFOLDER" Name="F-Spot">
          <Component Id="MainExecutable">
            <File Id="FSpotExe" Source="bin\f-spot.exe" />
            <File Id="FSpotConfig" Source="bin\f-spot.exe.config" />
          </Component>
        </Directory>
      </Directory>
    </Directory>
    
    <Feature Id="ProductFeature" Title="F-Spot" Level="1">
      <ComponentRef Id="MainExecutable" />
    </Feature>
  </Product>
</Wix>
```

**Chocolatey Package:**
```powershell
# tools/chocolateyinstall.ps1
$packageName = 'f-spot'
$installerType = 'msi'
$url = 'https://github.com/f-spot/f-spot/releases/download/v0.9.0/f-spot-0.9.0.msi'
$checksum = '...'

Install-ChocolateyPackage $packageName $installerType "" $url -checksum $checksum
```

### Flatpak Distribution

**Flatpak Manifest:**
```json
{
    "app-id": "org.gnome.FSpot",
    "runtime": "org.gnome.Platform",
    "runtime-version": "42",
    "sdk": "org.gnome.Sdk",
    "command": "f-spot",
    "finish-args": [
        "--share=ipc",
        "--socket=x11",
        "--filesystem=home",
        "--filesystem=xdg-pictures"
    ],
    "modules": [
        {
            "name": "f-spot",
            "buildsystem": "simple",
            "build-commands": [
                "./autogen.sh --prefix=/app",
                "make",
                "make install"
            ],
            "sources": [
                {
                    "type": "archive",
                    "url": "https://github.com/f-spot/f-spot/archive/v0.9.0.tar.gz"
                }
            ]
        }
    ]
}
```

## Troubleshooting Common Build Issues

### Issue 1: GTK# Not Found

**Symptoms:**
- `Could not load file or assembly 'gtk-sharp'`
- Build fails with GTK# references

**Solutions:**

**Linux:**
```bash
# Install GTK# development packages
sudo apt install libgtk2.0-cil-dev gtk-sharp2-gapi

# Verify installation
pkg-config --modversion gtk-sharp-2.0
```

**Windows:**
```powershell
# Reinstall GTK# for .NET
choco uninstall gtksharp
choco install gtksharp

# Verify GAC registration
gacutil -l gtk-sharp
```

### Issue 2: Mono Certificate Issues

**Symptoms:**
- NuGet restore fails with SSL/TLS errors
- HTTPS connections fail

**Solution:**
```bash
# Sync certificates
sudo cert-sync /etc/ssl/certs/ca-certificates.crt

# Alternative certificate locations:
# Fedora/RHEL: /etc/pki/tls/certs/ca-bundle.crt
# macOS: /System/Library/OpenSSL/cert.pem
```

### Issue 3: libfspot.so Build Failures

**Symptoms:**
- Native library compilation errors
- Missing development headers

**Solutions:**
```bash
# Install native development packages
sudo apt install build-essential libtool automake
sudo apt install libglib2.0-dev libgtk2.0-dev liblcms2-dev

# Force rebuild native library
cd lib/libfspot
make clean
make
sudo make install
```

### Issue 4: MSBuild Platform Issues

**Symptoms:**
- Platform mismatch errors
- x86/x64 architecture conflicts

**Solutions:**
```bash
# Force platform
msbuild build.proj -p:Platform=x86

# Clean and rebuild
msbuild build.proj -t:Clean
msbuild build.proj -t:Rebuild
```

### Issue 5: Missing Dependencies

**Diagnostic Commands:**
```bash
# Check Mono installation
mono --version
mcs --version

# Check GTK# installation
pkg-config --list-all | grep sharp

# Check native dependencies
ldd bin/f-spot.exe  # Linux
otool -L bin/f-spot.exe  # macOS
```

### Issue 6: Autotools Configuration Failures

**Common Error Messages and Solutions:**

**`configure: error: Package requirements (gtk-sharp-2.0 >= 2.12.2) were not met`**
```bash
# Install GTK# development package
sudo apt install libgtk2.0-cil-dev

# Or specify PKG_CONFIG_PATH
export PKG_CONFIG_PATH=/usr/lib/pkgconfig:$PKG_CONFIG_PATH
./autogen.sh
```

**`aclocal: macro not found`**
```bash
# Install required autotools
sudo apt install autotools-dev intltool

# Regenerate autotools files
autoreconf -fiv
```

## Advanced Build Scenarios

### Custom Configuration Builds

**Debug Build with Additional Logging:**
```bash
# MSBuild debug build
msbuild build.proj -p:Configuration=Debug -p:DefineConstants="DEBUG;TRACE;VERBOSE_LOGGING"

# Autotools debug build
./autogen.sh --enable-debug --enable-tests --prefix=/usr/local
CFLAGS=-g CXXFLAGS=-g make
```

**Release Build with Optimizations:**
```bash
# MSBuild optimized release
msbuild build.proj -p:Configuration=Release -p:Optimize=true -p:DebugType=none

# Autotools optimized release
./autogen.sh --enable-release --disable-debug
CFLAGS="-O3 -DNDEBUG" make
```

### Performance Profiling Builds

**Build with Profiling Support:**
```bash
# Mono profiler support
msbuild build.proj -p:Configuration=Debug -p:DefineConstants="PROFILING_ENABLED"

# Run with profiler
mono --profile=log:heapshot=ondemand bin/f-spot.exe
```

### Memory Debugging Builds

**Valgrind-Compatible Build:**
```bash
# Configure for memory debugging
./autogen.sh --enable-debug --enable-tests
CFLAGS="-g -O0" make

# Run with Valgrind
G_SLICE=always-malloc G_DEBUG=gc-friendly \
valgrind --tool=memcheck --leak-check=full \
mono bin/f-spot.exe
```

### Static Analysis Integration

**Build with Code Analysis:**
```xml
<!-- Directory.Build.props addition -->
<PropertyGroup>
  <RunAnalyzersDuringBuild>true</RunAnalyzersDuringBuild>
  <TreatWarningsAsErrors>true</TreatWarningsAsErrors>
  <WarningsAsErrors />
  <WarningsNotAsErrors>CS0618;CS0612</WarningsNotAsErrors>
</PropertyGroup>
```

### Custom Extension Development

**Building with Custom Extensions:**
```bash
# Create extension directory
mkdir src/Extensions/MyExtension

# Build specific extension
msbuild src/Extensions/MyExtension/MyExtension.csproj

# Install extension
cp bin/Extensions/MyExtension.dll ~/.config/f-spot/addins/
```

## Dependency Management

### NuGet Package Management

**Centralized Package Management:**
F-Spot uses centralized package version management through `Directory.Packages.props`:

```xml
<Project>
  <ItemGroup>
    <PackageVersion Include="TagLibSharp" Version="2.2.0" />
    <PackageVersion Include="Serilog" Version="2.10.0" />
    <PackageVersion Include="Newtonsoft.Json" Version="13.0.1" />
  </ItemGroup>
</Project>
```

**Package Update Process:**
```bash
# Check for outdated packages
dotnet list package --outdated

# Update specific package
# Edit Directory.Packages.props with new version
# Then restore
dotnet restore
```

### Native Dependency Management

**pkg-config Integration:**
```bash
# Check available packages
pkg-config --list-all | grep -E "(gtk|glib|mono)"

# Verify specific versions
pkg-config --modversion gtk+-2.0
pkg-config --modversion glib-2.0
```

**Library Path Configuration:**
```bash
# Set library paths (if needed)
export LD_LIBRARY_PATH=/usr/local/lib:$LD_LIBRARY_PATH
export PKG_CONFIG_PATH=/usr/local/lib/pkgconfig:$PKG_CONFIG_PATH

# macOS library paths
export DYLD_LIBRARY_PATH=/usr/local/lib:$DYLD_LIBRARY_PATH
```

### Build Environment Validation

**Comprehensive Environment Check Script:**
```bash
#!/bin/bash
# validate-build-env.sh

echo "=== Build Environment Validation ==="

# Check Mono/MSBuild
command -v mono >/dev/null 2>&1 || { echo "Mono not found"; exit 1; }
command -v msbuild >/dev/null 2>&1 || { echo "MSBuild not found"; exit 1; }

echo "Mono version: $(mono --version | head -1)"

# Check GTK#
if pkg-config --exists gtk-sharp-2.0; then
    echo "GTK# version: $(pkg-config --modversion gtk-sharp-2.0)"
else
    echo "WARNING: GTK# not found via pkg-config"
fi

# Check native dependencies
for lib in gtk+-2.0 glib-2.0 lcms2; do
    if pkg-config --exists $lib; then
        echo "$lib version: $(pkg-config --modversion $lib)"
    else
        echo "WARNING: $lib not found"
    fi
done

echo "=== Environment OK ==="
```

This comprehensive build system guide provides detailed instructions for building F-Spot across all supported platforms, setting up development environments, handling cross-compilation scenarios, understanding the CI/CD pipeline, creating distribution packages, troubleshooting common issues, and implementing advanced build configurations.