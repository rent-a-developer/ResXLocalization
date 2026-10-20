global using System.Globalization;
global using System.Resources;
global using AwesomeAssertions;
global using RentADeveloper.ResXLocalization.Core.Tests;
global using Xunit;
// An alias rather than the namespace: Jeffijoe.MessageFormat declares an IMessageFormatter of its own, which
// would make RentADeveloper.ResXLocalization.IMessageFormatter ambiguous.
global using JeffijoeMessageFormatter = Jeffijoe.MessageFormat.MessageFormatter;
