// Hand-authored accessor mirroring what PublicResXFileCodeGenerator emits (trimmed to what the
// consumption test needs). The packaged source generator pairs Strings.resx with this sibling
// file to emit the typed StringsKeys class.

#nullable enable

namespace WpfConsumer.Resources
{
    public class Strings
    {
        private static global::System.Resources.ResourceManager? resourceMan;

        internal Strings()
        {
        }

        public static global::System.Resources.ResourceManager ResourceManager =>
            resourceMan ??= new global::System.Resources.ResourceManager("WpfConsumer.Resources.Strings", typeof(Strings).Assembly);
    }
}
