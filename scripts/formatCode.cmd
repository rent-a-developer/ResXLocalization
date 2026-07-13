@echo off
rem Formats the whole repository: C# whitespace via dotnet format, XAML via XamlStyler.
rem (Semantic style and analyzer rules are enforced by the build itself; dotnet format's
rem full analyzer mode is unreliable against multi-targeted projects.)
rem Always operates on the repository root, regardless of the caller's working directory.
cd /d "%~dp0.." || exit /b 1
dotnet tool restore --configfile NuGet.config || exit /b 1
dotnet format whitespace ResXLocalization.slnx || exit /b 1
dotnet xstyler --recursive --directory src || exit /b 1
dotnet xstyler --recursive --directory samples || exit /b 1
