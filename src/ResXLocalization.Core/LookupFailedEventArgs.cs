namespace RentADeveloper.ResXLocalization;

/// <summary>Provides the failed lookup reported through <see cref="ILocalizer.LookupFailed" />.</summary>
/// <param name="failure">The failed lookup.</param>
public sealed class LookupFailedEventArgs(LookupFailure failure) : EventArgs
{
    /// <summary>Gets the failed lookup.</summary>
    public LookupFailure Failure { get; } = failure;
}
