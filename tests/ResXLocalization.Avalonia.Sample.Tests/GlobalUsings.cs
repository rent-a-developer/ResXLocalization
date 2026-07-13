global using System.Globalization;
global using System.Resources;
global using Avalonia;
global using Avalonia.Controls;
global using Avalonia.Data;
global using Avalonia.Headless;
global using Avalonia.Headless.XUnit;
global using Avalonia.Markup.Xaml;
global using Avalonia.Threading;
global using Avalonia.VisualTree;
global using AwesomeAssertions;
global using RentADeveloper.ResXLocalization.Avalonia.Sample.Models;
global using RentADeveloper.ResXLocalization.Avalonia.Sample.Resources;
global using RentADeveloper.ResXLocalization.Avalonia.Sample.Tests;
global using RentADeveloper.ResXLocalization.Avalonia.Sample.ViewModels;
global using RentADeveloper.ResXLocalization.Avalonia.Sample.Views;
global using Xunit;

// The localizer is a process-wide singleton and the static resource managers are shared, so tests
// must not run in parallel. [AvaloniaFact] already marshals every test onto the single headless UI
// thread, which serializes them; this makes that guarantee explicit and mirrors the WPF suite.
[assembly: CollectionBehavior(DisableTestParallelization = true)]
