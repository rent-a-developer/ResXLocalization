namespace RentADeveloper.ResXLocalization;

/// <summary>Why a lookup failed; see <see cref="LookupFailure.Reason" />.</summary>
public enum LookupFailureReason
{
    /// <summary>
    /// No resource file contains the key, across the complete parent and neutral fallback chain.
    /// </summary>
    TranslationNotFound = 0,

    /// <summary>The value was found, but the <see cref="ILocalizer.MessageFormatter" /> threw.</summary>
    MessageFormatFailed = 1,
}
