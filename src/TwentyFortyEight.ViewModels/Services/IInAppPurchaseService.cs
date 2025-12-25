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

/// <summary>
/// Platform-agnostic service for in-app purchases.
/// Platform-specific implementations should integrate with:
/// - Android: Google Play Billing
/// - iOS: StoreKit
/// - Windows: Microsoft Store
/// </summary>
public interface IInAppPurchaseService
{
    /// <summary>
    /// Gets whether in-app purchases are supported on this platform.
    /// </summary>
    bool IsSupported { get; }

    /// <summary>
    /// The product ID for the "Remove Ads" purchase.
    /// </summary>
    string RemoveAdsProductId { get; }

    /// <summary>
    /// Initializes the in-app purchase service. Should be called once at app startup.
    /// </summary>
    Task InitializeAsync();

    /// <summary>
    /// Gets the price of a product for display purposes.
    /// </summary>
    /// <param name="productId">The product identifier.</param>
    /// <returns>The formatted price string, or null if unavailable.</returns>
    Task<string?> GetPriceAsync(string productId);

    /// <summary>
    /// Initiates a purchase for the specified product.
    /// </summary>
    /// <param name="productId">The product identifier.</param>
    /// <returns>The result of the purchase operation.</returns>
    Task<PurchaseResult> PurchaseAsync(string productId);

    /// <summary>
    /// Checks if a product has already been purchased.
    /// </summary>
    /// <param name="productId">The product identifier.</param>
    /// <returns>True if the product has been purchased.</returns>
    Task<bool> IsProductPurchasedAsync(string productId);

    /// <summary>
    /// Restores previously made purchases (primarily for iOS).
    /// </summary>
    /// <returns>True if any purchases were restored.</returns>
    Task<bool> RestorePurchasesAsync();
}
