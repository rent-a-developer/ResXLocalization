namespace RentADeveloper.ResXLocalization.Core.Tests;

/// <summary>
/// Exercises the <see cref="MessageArguments" /> factory: every argument has its index as position,
/// names come from tuples, dictionaries and properties, and duplicate names are rejected.
/// </summary>
public class MessageArgumentsTests
{
    [Fact]
    public void DuplicateNames_AreRejectedOrdinally()
    {
        var tuples = () => MessageArguments.FromTuples(("A", 1), ("A", 2));
        var hiddenProperty = () => MessageArguments.FromObject(new Derived());

        tuples.Should().Throw<ArgumentException>().WithParameterName("arguments").Which.Message.Should().Contain("A");
        MessageArguments
            .FromTuples(("A", 1), ("a", 2))
            .Should()
            .Equal(new MessageArgument(0, "A", 1), new MessageArgument(1, "a", 2));
        hiddenProperty
            .Should()
            .Throw<ArgumentException>()
            .WithParameterName("source")
            .Which.Message.Should()
            .Contain("A");
    }

    [Fact]
    public void EmptyNames_AreRejected()
    {
        var tuples = () => MessageArguments.FromTuples(("", 1));
        var dictionary = () => MessageArguments.FromDictionary(new Dictionary<string, object?> { [""] = 1 });

        tuples.Should().Throw<ArgumentException>().WithParameterName("arguments");
        dictionary.Should().Throw<ArgumentException>().WithParameterName("arguments");
    }

    [Fact]
    public void Empty_HasNoArguments_AndInstancesHaveNoIndexer()
    {
        MessageArguments.Empty.Should().BeEmpty().And.BeSameAs(MessageArguments.Empty);

        var arguments = MessageArguments.FromValues("x", 2);

        arguments.Count.Should().Be(2);
        arguments.Should().NotBeAssignableTo<IReadOnlyList<MessageArgument>>();
        typeof(MessageArguments).GetProperty("Item").Should().BeNull();
    }

    [Fact]
    public void EveryFactory_AssignsPositionsInInputOrder()
    {
        MessageArguments
            .FromValues(1, null, "x")
            .Should()
            .Equal(
                new MessageArgument(0, null, 1),
                new MessageArgument(1, null, null),
                new MessageArgument(2, null, "x")
            );
        MessageArguments.FromValues().Should().BeEmpty();
        MessageArguments
            .FromTuples(("A", 1), ("B", null))
            .Should()
            .Equal(new MessageArgument(0, "A", 1), new MessageArgument(1, "B", null));
        MessageArguments
            .FromDictionary(new Dictionary<string, object?> { ["A"] = 1, ["B"] = 2 })
            .Should()
            .Equal(new MessageArgument(0, "A", 1), new MessageArgument(1, "B", 2));
        MessageArguments
            .FromObject(new { A = 1, B = "x" })
            .Should()
            .Equal(new MessageArgument(0, "A", 1), new MessageArgument(1, "B", "x"));
    }

    [Fact]
    public void FromArguments_KeepsExplicitPositions_AndRejectsPositionsOutsideZeroToCountMinusOne()
    {
        var negative = () => MessageArguments.FromArguments(new MessageArgument(-1, null, "x"));
        var gap = () => MessageArguments.FromArguments(new MessageArgument(1, null, "x"));
        var repeated = () => MessageArguments.FromArguments(new(0, null, "x"), new(0, null, "y"));

        MessageArguments
            .FromArguments(new(1, null, "b"), new(0, "A", "a"))
            .Should()
            .Equal(new MessageArgument(1, null, "b"), new MessageArgument(0, "A", "a"));
        negative.Should().Throw<ArgumentException>().WithParameterName("arguments");
        gap.Should().Throw<ArgumentException>().WithParameterName("arguments");
        repeated.Should().Throw<ArgumentException>().WithParameterName("arguments");
    }

    [Fact]
    public void FromObject_ReadsOnlyThePublicInstancePropertiesOfTheStaticType()
    {
        MessageArguments.FromObject(new WithStaticAndIndexer()).Should().Equal(new MessageArgument(0, "A", 7));
        MessageArguments.FromObject<object>(new { A = 1 }).Should().BeEmpty();
    }

    [Fact]
    public void FromTuples_MixesNamedAndPositionalArguments_WithNullNames() =>
        MessageArguments
            .FromTuples((null, "a.txt"), ("Total", 10), (null, 3))
            .Should()
            .Equal(
                new MessageArgument(0, null, "a.txt"),
                new MessageArgument(1, "Total", 10),
                new MessageArgument(2, null, 3)
            );

    [Fact]
    public void NullInput_Throws()
    {
        ((Action)(() => MessageArguments.FromValues((object?[])null!))).Should().Throw<ArgumentNullException>();
        ((Action)(() => MessageArguments.FromTuples(null!))).Should().Throw<ArgumentNullException>();
        ((Action)(() => MessageArguments.FromDictionary(null!))).Should().Throw<ArgumentNullException>();
        ((Action)(() => MessageArguments.FromObject<object>(null!))).Should().Throw<ArgumentNullException>();
    }

    /// <summary>Declares <c>A</c> as an <see cref="int" />.</summary>
    public class Base
    {
        /// <summary>Gets the base value.</summary>
        public int A { get; } = 1;
    }

    /// <summary>Hides <c>A</c> with a different property type, so reflection returns both.</summary>
    public sealed class Derived : Base
    {
        /// <summary>Gets the derived value.</summary>
        public new string A { get; } = "derived";
    }

    /// <summary>One instance property, one static property and an indexer.</summary>
    public sealed class WithStaticAndIndexer
    {
        /// <summary>Gets a static value that must not become an argument.</summary>
        public static int S { get; } = 2;

        /// <summary>Gets the instance value.</summary>
        public int A { get; } = 7;

        /// <summary>Gets an indexed value that must not become an argument.</summary>
        /// <param name="index">The index.</param>
        public int this[int index] => index + this.A;
    }
}
