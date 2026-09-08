// Hand-authored accessor mirroring what PublicResXFileCodeGenerator emits (trimmed to what the
// consumption test needs). The packaged source generator pairs Catalog.resx with this sibling
// file to emit the typed CatalogKeys class.

#nullable enable

namespace AvaloniaConsumer.Resources
{
    public class Catalog
    {
        private static global::System.Resources.ResourceManager? resourceMan;

        internal Catalog()
        {
        }

        public static global::System.Resources.ResourceManager ResourceManager =>
            resourceMan ??= new global::System.Resources.ResourceManager("AvaloniaConsumer.Resources.Catalog", typeof(Catalog).Assembly);
    }
}
