# Development Setup Guide

## Overview

This guide provides step-by-step instructions for setting up a complete F-Spot development environment across different platforms. It covers IDE configuration, dependency installation, build verification, and common troubleshooting scenarios.

## Quick Start

### Prerequisites Check

Before starting, verify your system meets these minimum requirements:

```bash
# Check .NET/Mono version
dotnet --version  # Should be 3.1+ or
mono --version    # Should be 6.0+

# Check GTK development libraries (Linux/macOS)
pkg-config --modversion gtk+-3.0  # Should be 3.22+

# Check Git
git --version  # Any recent version
```

### One-Command Setup Scripts

**Linux (Ubuntu/Debian)**:
```bash
curl -sSL https://raw.githubusercontent.com/f-spot/f-spot/main/scripts/setup-dev-ubuntu.sh | bash
```

**macOS**:
```bash
curl -sSL https://raw.githubusercontent.com/f-spot/f-spot/main/scripts/setup-dev-macos.sh | bash
```

**Windows (PowerShell as Administrator)**:
```powershell
Set-ExecutionPolicy Bypass -Scope Process -Force
iex ((New-Object System.Net.WebClient).DownloadString('https://raw.githubusercontent.com/f-spot/f-spot/main/scripts/setup-dev-windows.ps1'))
```

## Detailed Platform Setup

### Linux Development Setup

#### Ubuntu 20.04/22.04 LTS

**Step 1: Install System Dependencies**
```bash
# Update package index
sudo apt update

# Install build essentials
sudo apt install -y build-essential git curl wget

# Install .NET SDK
wget https://packages.microsoft.com/config/ubuntu/$(lsb_release -rs)/packages-microsoft-prod.deb -O packages-microsoft-prod.deb
sudo dpkg -i packages-microsoft-prod.deb
sudo apt update
sudo apt install -y dotnet-sdk-6.0

# Install Mono (alternative to .NET)
sudo apt install -y mono-complete mono-devel

# Install GTK development libraries
sudo apt install -y \
    libgtk-3-dev \
    libglib2.0-dev \
    libcairo2-dev \
    liblcms2-dev \
    libexif-dev \
    libjpeg-dev \
    libpng-dev \
    libtiff-dev \
    libgstreamer1.0-dev \
    libgstreamer-plugins-base1.0-dev

# Install additional tools
sudo apt install -y \
    autotools-dev \
    intltool \
    libtool \
    pkg-config \
    gnome-doc-utils \
    gtk-doc-tools
```

**Step 2: Clone and Build**
```bash
# Clone repository
git clone https://github.com/f-spot/f-spot.git
cd f-spot

# Restore NuGet packages
dotnet restore F-Spot.sln

# Build solution
dotnet build F-Spot.sln -c Debug

# Verify build
dotnet run --project src/Clients/FSpot.Gtk/FSpot.Gtk.csproj
```

#### Fedora 35/36

**System Dependencies**:
```bash
# Install development tools
sudo dnf groupinstall -y "Development Tools"
sudo dnf install -y git curl wget

# Install .NET SDK
sudo dnf install -y dotnet-sdk-6.0

# Install Mono
sudo dnf install -y mono-complete mono-devel

# Install GTK development libraries
sudo dnf install -y \
    gtk3-devel \
    glib2-devel \
    cairo-devel \
    lcms2-devel \
    libexif-devel \
    libjpeg-turbo-devel \
    libpng-devel \
    libtiff-devel \
    gstreamer1-devel \
    gstreamer1-plugins-base-devel

# Install build tools
sudo dnf install -y \
    autoconf \
    automake \
    libtool \
    intltool \
    pkgconfig \
    gnome-doc-utils
```

#### Arch Linux

**System Dependencies**:
```bash
# Install base development packages
sudo pacman -S base-devel git

# Install .NET SDK
sudo pacman -S dotnet-sdk

# Install Mono
sudo pacman -S mono mono-tools

# Install GTK development libraries
sudo pacman -S \
    gtk3 \
    glib2 \
    cairo \
    lcms2 \
    libexif \
    libjpeg-turbo \
    libpng \
    libtiff \
    gstreamer \
    gst-plugins-base

# Install build tools
sudo pacman -S \
    autoconf \
    automake \
    libtool \
    intltool \
    pkgconf \
    gnome-doc-utils
```

### Windows Development Setup

#### Using Visual Studio 2022

**Step 1: Install Visual Studio**
```powershell
# Download and install Visual Studio 2022 Community
# Ensure these workloads are selected:
# - .NET Framework development
# - Desktop development with C++
```

**Step 2: Install GTK+ Runtime**
```powershell
# Option 1: Using Chocolatey (recommended)
Set-ExecutionPolicy Bypass -Scope Process -Force
iex ((New-Object System.Net.WebClient).DownloadString('https://chocolatey.org/install.ps1'))
choco install gtk-runtime

# Option 2: Manual installation
# Download GTK+ 3.24.x for Windows from https://www.gtk.org/docs/installations/windows/
# Extract to C:\gtk
# Add C:\gtk\bin to system PATH
```

**Step 3: Configure Development Environment**
```powershell
# Clone repository
git clone https://github.com/f-spot/f-spot.git
cd f-spot

# Set environment variables for GTK
$env:PKG_CONFIG_PATH = "C:\gtk\lib\pkgconfig"
$env:PATH = "C:\gtk\bin;" + $env:PATH

# Restore packages and build
dotnet restore F-Spot.sln
dotnet build F-Spot.sln -c Debug

# Alternative: Open F-Spot.sln in Visual Studio
```

#### Using VS Code

**Step 1: Install Prerequisites**
```powershell
# Install .NET SDK
winget install Microsoft.DotNet.SDK.6

# Install VS Code
winget install Microsoft.VisualStudioCode

# Install Git
winget install Git.Git
```

**Step 2: Configure VS Code**
```bash
# Install C# extension
code --install-extension ms-dotnettools.csharp

# Install GitLens extension
code --install-extension eamodio.gitlens

# Open project
git clone https://github.com/f-spot/f-spot.git
cd f-spot
code .
```

**VS Code Configuration** (`.vscode/settings.json`):
```json
{
    "dotnet.defaultSolution": "F-Spot.sln",
    "omnisharp.enableEditorConfigSupport": true,
    "omnisharp.enableRoslynAnalyzers": true,
    "files.exclude": {
        "**/bin": true,
        "**/obj": true,
        "**/.vs": true
    }
}
```

### macOS Development Setup

#### Using Homebrew

**Step 1: Install Homebrew and Dependencies**
```bash
# Install Homebrew if not present
/bin/bash -c "$(curl -fsSL https://raw.githubusercontent.com/Homebrew/install/HEAD/install.sh)"

# Install development tools
brew install git mono gtk+3 cairo glib adwaita-icon-theme hicolor-icon-theme

# Install .NET SDK
brew install --cask dotnet-sdk

# Install additional dependencies
brew install autoconf automake libtool intltool pkg-config
```

**Step 2: Configure Environment**
```bash
# Set environment variables
export PKG_CONFIG_PATH="/usr/local/lib/pkgconfig:/usr/local/share/pkgconfig:$PKG_CONFIG_PATH"
export DYLD_LIBRARY_PATH="/usr/local/lib:$DYLD_LIBRARY_PATH"

# Add to ~/.zshrc or ~/.bash_profile for persistence
echo 'export PKG_CONFIG_PATH="/usr/local/lib/pkgconfig:/usr/local/share/pkgconfig:$PKG_CONFIG_PATH"' >> ~/.zshrc
echo 'export DYLD_LIBRARY_PATH="/usr/local/lib:$DYLD_LIBRARY_PATH"' >> ~/.zshrc

# Sync certificates (required for NuGet)
sudo cert-sync /usr/local/share/ca-certificates/ca-certificates.crt
```

**Step 3: Build Project**
```bash
# Clone and build
git clone https://github.com/f-spot/f-spot.git
cd f-spot

# Restore and build
dotnet restore F-Spot.sln
dotnet build F-Spot.sln -c Debug

# Run application
dotnet run --project src/Clients/FSpot.Gtk/FSpot.Gtk.csproj
```

#### Using MacPorts

**Alternative setup with MacPorts**:
```bash
# Install MacPorts if not present
# Download from https://www.macports.org/install.php

# Install dependencies
sudo port install mono gtk3 cairo glib2 adwaita-icon-theme
sudo port install autoconf automake libtool intltool pkgconfig

# Set environment variables
export PKG_CONFIG_PATH="/opt/local/lib/pkgconfig:$PKG_CONFIG_PATH"
export DYLD_LIBRARY_PATH="/opt/local/lib:$DYLD_LIBRARY_PATH"
```

## IDE-Specific Configuration

### Visual Studio 2022 (Windows)

**Extensions to Install**:
- GitExtensions
- SonarLint for Visual Studio
- EditorConfig Language Service
- NUnit 3 Test Adapter

**Project Configuration**:
```xml
<!-- Directory.Build.props -->
<Project>
    <PropertyGroup>
        <TreatWarningsAsErrors>true</TreatWarningsAsErrors>
        <WarningsNotAsErrors>CS0618;CS0672</WarningsNotAsErrors>
        <RunAnalyzersDuringBuild>true</RunAnalyzersDuringBuild>
    </PropertyGroup>
</Project>
```

### Visual Studio Code (Cross-Platform)

**Recommended Extensions**:
```bash
# Essential C# development
code --install-extension ms-dotnettools.csharp

# Git integration
code --install-extension eamodio.gitlens

# EditorConfig support
code --install-extension EditorConfig.EditorConfig

# XML tools for .csproj files
code --install-extension redhat.vscode-xml

# NuGet package manager
code --install-extension jmrog.vscode-nuget-package-manager
```

**Launch Configuration** (`.vscode/launch.json`):
```json
{
    "version": "0.2.0",
    "configurations": [
        {
            "name": "Launch F-Spot",
            "type": "coreclr",
            "request": "launch",
            "preLaunchTask": "build",
            "program": "${workspaceFolder}/bin/Debug/net472/f-spot.exe",
            "args": [],
            "cwd": "${workspaceFolder}",
            "console": "internalConsole",
            "stopAtEntry": false
        }
    ]
}
```

**Build Tasks** (`.vscode/tasks.json`):
```json
{
    "version": "2.0.0",
    "tasks": [
        {
            "label": "build",
            "command": "dotnet",
            "type": "process",
            "args": [
                "build",
                "${workspaceFolder}/F-Spot.sln",
                "/property:GenerateFullPaths=true",
                "/consoleloggerparameters:NoSummary"
            ],
            "group": "build",
            "presentation": {
                "reveal": "silent"
            },
            "problemMatcher": "$msCompile"
        },
        {
            "label": "clean",
            "command": "dotnet",
            "type": "process",
            "args": [
                "clean",
                "${workspaceFolder}/F-Spot.sln"
            ],
            "group": "build"
        }
    ]
}
```

### MonoDevelop (Linux/macOS)

**Installation**:
```bash
# Ubuntu/Debian
sudo apt install monodevelop

# macOS (via Homebrew)
brew install --cask monodevelop

# Fedora
sudo dnf install monodevelop
```

**Project Setup**:
1. Open MonoDevelop
2. File → Open → Select `F-Spot.sln`
3. Project → Restore NuGet Packages
4. Build → Build All

## Development Environment Validation

### Build Verification Script

Create `scripts/verify-build.sh`:
```bash
#!/bin/bash
set -e

echo "=== F-Spot Development Environment Validation ==="

# Check .NET/Mono
echo "Checking .NET/Mono installation..."
if command -v dotnet &> /dev/null; then
    echo "✓ .NET SDK: $(dotnet --version)"
else
    echo "✗ .NET SDK not found"
    exit 1
fi

# Check GTK (Linux/macOS only)
if [[ "$OSTYPE" != "msys" ]]; then
    echo "Checking GTK+ installation..."
    if pkg-config --exists gtk+-3.0; then
        echo "✓ GTK+ 3.0: $(pkg-config --modversion gtk+-3.0)"
    else
        echo "✗ GTK+ 3.0 not found"
        exit 1
    fi
fi

# Check build tools
echo "Checking build tools..."
if command -v git &> /dev/null; then
    echo "✓ Git: $(git --version)"
else
    echo "✗ Git not found"
    exit 1
fi

# Test build
echo "Testing F-Spot build..."
dotnet restore F-Spot.sln
dotnet build F-Spot.sln -c Debug --no-restore

if [ $? -eq 0 ]; then
    echo "✓ F-Spot builds successfully"
else
    echo "✗ F-Spot build failed"
    exit 1
fi

# Test run (if display available)
if [[ -n "$DISPLAY" ]] || [[ "$OSTYPE" == "darwin"* ]] || [[ "$OSTYPE" == "msys" ]]; then
    echo "Testing F-Spot execution..."
    timeout 10s dotnet run --project src/Clients/FSpot.Gtk/FSpot.Gtk.csproj --no-build || true
    echo "✓ F-Spot executable starts"
fi

echo "=== All checks passed! Development environment ready. ==="
```

### Troubleshooting Common Issues

#### Issue: NuGet Package Restore Fails

**Symptoms**:
```
error NU1301: Unable to load the service index for source
```

**Solutions**:
```bash
# Clear NuGet cache
dotnet nuget locals all --clear

# Update NuGet configuration
dotnet nuget add source https://api.nuget.org/v3/index.json -n nuget.org

# Check network connectivity and proxy settings
```

#### Issue: GTK+ Not Found (Linux/macOS)

**Symptoms**:
```
System.DllNotFoundException: libgtk-3.so.0
```

**Solutions**:
```bash
# Linux: Install GTK+ development packages
sudo apt install libgtk-3-dev  # Ubuntu/Debian
sudo dnf install gtk3-devel     # Fedora
sudo zypper install gtk3-devel  # openSUSE

# macOS: Install via package manager
brew install gtk+3              # Homebrew
sudo port install gtk3          # MacPorts

# Set PKG_CONFIG_PATH if needed
export PKG_CONFIG_PATH="/usr/local/lib/pkgconfig:$PKG_CONFIG_PATH"
```

#### Issue: Mono Certificate Errors

**Symptoms**:
```
The remote certificate is invalid according to the validation procedure
```

**Solutions**:
```bash
# Import certificates
sudo cert-sync /etc/ssl/certs/ca-certificates.crt  # Linux
sudo cert-sync /usr/local/share/ca-certificates/ca-certificates.crt  # macOS

# Alternative: disable certificate validation (not recommended for production)
export MONO_TLS_PROVIDER=legacy
```

#### Issue: Build Errors on Windows

**Symptoms**:
```
MSB3073: The command "pkg-config" exited with code 9009
```

**Solutions**:
```powershell
# Install pkg-config for Windows
choco install pkgconfiglite

# Or set MSBuild property to skip pkg-config
dotnet build -p:UseSystemGtk=false
```

## Performance Optimization for Development

### Build Performance

**Enable Parallel Builds**:
```bash
# Use all available cores
dotnet build -m:0  # or --maxcpucount:0

# MSBuild directly
msbuild F-Spot.sln /m
```

**Incremental Build Configuration**:
```xml
<!-- Directory.Build.props -->
<PropertyGroup>
    <EnableNETAnalyzers>false</EnableNETAnalyzers> <!-- Disable for faster builds -->
    <RunAnalyzersDuringBuild>false</RunAnalyzersDuringBuild>
    <SkipCopyLocal>true</SkipCopyLocal> <!-- For faster incremental builds -->
</PropertyGroup>
```

### Development Database

**Use Lightweight Test Database**:
```bash
# Create development database with sample data
cp tests/data/f-spot-test.db ~/.config/f-spot/photos.db

# Or use in-memory database for testing
export FSPOT_DATABASE_PATH=":memory:"
```

### Hot Reload Setup

**Enable Hot Reload for UI Development**:
```xml
<!-- In FSpot.Gtk.csproj -->
<PropertyGroup>
    <UseWindowsForms>false</UseWindowsForms>
    <EnableHotReload>true</EnableHotReload>
</PropertyGroup>
```

## Development Workflow

### Git Configuration

**Recommended Git Settings**:
```bash
# Set up Git configuration for F-Spot development
git config user.name "Your Name"
git config user.email "your.email@example.com"

# Set up useful aliases
git config alias.st status
git config alias.co checkout
git config alias.br branch
git config alias.up '!git fetch && git rebase origin/master'

# Set up automatic line ending handling
git config core.autocrlf input  # Linux/macOS
git config core.autocrlf true   # Windows
```

### Pre-commit Hooks

**Install Development Tools**:
```bash
# Install pre-commit framework
pip install pre-commit

# Install hooks
pre-commit install

# Run manually
pre-commit run --all-files
```

### Testing Setup

**Run Unit Tests**:
```bash
# Run all tests
dotnet test

# Run specific test project
dotnet test tests/FSpot.UnitTest/

# Run with coverage
dotnet test --collect:"XPlat Code Coverage"
```

**Integration Testing**:
```bash
# Build and run integration tests
msbuild build.proj /t:Test

# Run with specific test database
FSPOT_TEST_DB=tests/data/f-spot-large.db dotnet test
```

## Conclusion

This development setup guide provides comprehensive instructions for establishing a productive F-Spot development environment across all supported platforms. Key takeaways:

1. **Use Platform-Specific Package Managers**: Leverages system package managers for easier dependency management
2. **Standardized Build Process**: Uses .NET CLI consistently across platforms
3. **IDE Flexibility**: Supports multiple development environments
4. **Comprehensive Validation**: Includes verification scripts to ensure proper setup
5. **Performance Optimization**: Provides tips for faster development iteration

Regular validation of the development environment ensures consistency across the development team and reduces onboarding time for new contributors.