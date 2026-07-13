// ReSharper disable once CheckNamespace
namespace System.Runtime.CompilerServices;

/// <summary>
/// Polyfill for the marker type the C# compiler requires to compile <c>init</c> accessors and
/// <c>record</c> types; netstandard2.0 (this generator's target) does not ship it. The namespace
/// must be exactly <c>System.Runtime.CompilerServices</c> for the compiler to recognize it.
/// </summary>
internal static class IsExternalInit;
