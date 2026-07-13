global using System.Globalization;
global using System.Resources;
global using System.Windows;
global using System.Windows.Controls;
global using System.Windows.Data;
global using System.Windows.Threading;
global using AwesomeAssertions;
global using RentADeveloper.ResXLocalization.WPF.Sample.Models;
global using RentADeveloper.ResXLocalization.WPF.Sample.Resources;
global using RentADeveloper.ResXLocalization.WPF.Sample.ViewModels;
global using RentADeveloper.ResXLocalization.WPF.Sample.Views;
global using Xunit;

// WPF has thread affinity and the localizer is a process-wide singleton, so tests must not run in
// parallel. Every test body is marshalled onto the single shared STA/dispatcher thread (see WpfThread).
[assembly: CollectionBehavior(DisableTestParallelization = true)]
