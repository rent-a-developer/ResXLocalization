#!/usr/bin/env sh
# Formats the repository on non-Windows hosts: C# whitespace via dotnet format, XAML via XamlStyler.
# (Semantic style and analyzer rules are enforced by the build itself; dotnet format's full
# analyzer mode is unreliable against multi-targeted projects.)
# Uses the non-Windows solution filter because the WPF projects only load on Windows.
set -eu
cd "$(dirname "$0")/.."
dotnet tool restore --configfile NuGet.config
dotnet format whitespace ResXLocalization.NonWindows.slnf
dotnet xstyler --recursive --directory src
dotnet xstyler --recursive --directory samples
