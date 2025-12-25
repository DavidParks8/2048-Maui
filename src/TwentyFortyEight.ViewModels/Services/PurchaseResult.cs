namespace TwentyFortyEight.ViewModels.Services;

/// <summary>
/// Result of an in-app purchase operation.
/// </summary>
public enum PurchaseResult
{
    /// <summary>
    /// The purchase was completed successfully.
    /// </summary>
    Success,

    /// <summary>
    /// The user cancelled the purchase.
    /// </summary>
    Cancelled,

    /// <summary>
    /// The purchase failed due to an error.
    /// </summary>
    Failed,

    /// <summary>
    /// In-app purchases are not supported on this platform.
    /// </summary>
    NotSupported,

    /// <summary>
    /// The item has already been purchased.
    /// </summary>
    AlreadyOwned
}
