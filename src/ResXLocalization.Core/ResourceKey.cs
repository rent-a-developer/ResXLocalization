namespace RentADeveloper.ResXLocalization;

/// <summary>
/// A typed, file-scoped resource key that pairs a key name with the <see cref="ResourceManager" />
/// that owns it. The bundled source generator emits one static <see cref="ResourceKey" /> per string
/// entry in each eligible neutral <c>.resx</c> file, giving compile-time-validated keys with editor
/// auto-completion.
/// </summary>
/// <param name="Name">The resource key name as it appears in the <c>.resx</c> file.</param>
/// <param name="Manager">The resource manager that resolves <paramref name="Name" />.</param>
public readonly record struct ResourceKey(String Name, ResourceManager Manager);
