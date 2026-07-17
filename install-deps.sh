#!/bin/bash

# exit immediately if a command exits with a non-zero status
set -e

echo "============================================="
echo "   Todo Studio - Linux Dependency Installer  "
echo "============================================="

# Function to check if a command exists
command_exists() {
    command -v "$1" >/dev/null 2>&1
}

# Detect distribution
if [ -f /etc/arch-release ] || command_exists pacman; then
    echo "Detected Arch Linux or Arch-based distribution."
    echo "Installing required native packages using pacman..."
    sudo pacman -S --needed --noconfirm fontconfig xorg-server libx11 glu icu

elif [ -f /etc/debian_version ] || command_exists apt-get; then
    echo "Detected Debian/Ubuntu or Debian-based distribution."
    echo "Updating package lists and installing required native packages..."
    sudo apt-get update
    sudo apt-get install -y libfontconfig1 libx11-6 libglu1-mesa libicu-dev

elif [ -f /etc/fedora-release ] || command_exists dnf; then
    echo "Detected Fedora or Red Hat-based distribution."
    echo "Installing required native packages using dnf..."
    sudo dnf install -y fontconfig libX11 mesa-libGLU libicu

else
    echo "=============================================================="
    echo "WARNING: Could not automatically detect package manager!"
    echo "Please manually install equivalent packages for:"
    echo "  - fontconfig (Fonts configuration)"
    echo "  - libX11 / X11 Server (Window graphics)"
    echo "  - GLU / OpenGL (Skia rendering engine)"
    echo "  - ICU / libicu (Internationalization/Unicode Support)"
    echo "=============================================================="
    exit 1
fi

echo ""
echo "Success! All required native system dependencies are installed."
echo "============================================="
