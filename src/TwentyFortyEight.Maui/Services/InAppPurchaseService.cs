using Microsoft.Extensions.Logging;
using Plugin.InAppBilling;
using TwentyFortyEight.ViewModels.Services;

namespace TwentyFortyEight.Maui.Services;

/// <summary>
/// Implementation of IInAppPurchaseService using Plugin.InAppBilling for cross-platform support.
/// Supports Android (Google Play Billing), iOS (StoreKit), and Windows (Microsoft Store).
/// </summary>
public sealed partial class InAppPurchaseService(
    ISettingsService settingsService,
    IAdsService adsService,
    ILogger<InAppPurchaseService> logger) : IInAppPurchaseService
{
    private bool _isInitialized;

    /// <summary>
    /// Gets whether in-app purchases are supported on this platform.
    /// </summary>
    public bool IsSupported => true;

    /// <summary>
    /// Gets the product ID for removing ads.
    /// Implemented in platform-specific partial class files.
    /// </summary>
    public string RemoveAdsProductId => GetRemoveAdsProductId();

    /// <summary>
    /// Gets the platform-specific product ID for removing ads.
    /// Implemented in platform-specific partial class files.
    /// </summary>
    private static partial string GetRemoveAdsProductId();

    /// <summary>
    /// Initializes the in-app purchase service.
    /// </summary>
    public async Task InitializeAsync()
    {
        if (_isInitialized)
        {
            return;
        }

        try
        {
            // Check for and restore existing purchases
            await RestorePurchasesAsync();
            _isInitialized = true;
            LogIapInitialized();
        }
        catch (Exception ex)
        {
            LogIapInitializationFailed(ex);
        }
    }

    /// <summary>
    /// Gets the price of a product for display.
    /// </summary>
    public async Task<string?> GetPriceAsync(string productId)
    {
        try
        {
            var connected = await CrossInAppBilling.Current.ConnectAsync();
            if (!connected)
            {
                LogBillingConnectionFailed();
                return null;
            }

            var products = await CrossInAppBilling.Current.GetProductInfoAsync(
                ItemType.InAppPurchase,
                productId);

            var product = products?.FirstOrDefault();
            return product?.LocalizedPrice;
        }
        catch (InAppBillingPurchaseException ex)
        {
            LogGetPriceFailed(productId, ex);
            return null;
        }
        catch (Exception ex)
        {
            LogGetPriceFailed(productId, ex);
            return null;
        }
        finally
        {
            await CrossInAppBilling.Current.DisconnectAsync();
        }
    }

    /// <summary>
    /// Initiates a purchase for the specified product.
    /// </summary>
    public async Task<PurchaseResult> PurchaseAsync(string productId)
    {
        try
        {
            var connected = await CrossInAppBilling.Current.ConnectAsync();
            if (!connected)
            {
                LogBillingConnectionFailed();
                return PurchaseResult.Failed;
            }

            var purchase = await CrossInAppBilling.Current.PurchaseAsync(
                productId,
                ItemType.InAppPurchase);

            if (purchase == null)
            {
                LogPurchaseCancelled(productId);
                return PurchaseResult.Cancelled;
            }

            // Consume the purchase if needed (for consumables)
            // For non-consumables like "remove ads", we don't consume

            if (productId == RemoveAdsProductId)
            {
                adsService.DisableAds();
            }

            LogPurchaseSucceeded(productId);
            return PurchaseResult.Success;
        }
        catch (InAppBillingPurchaseException ex)
        {
            return HandlePurchaseException(productId, ex);
        }
        catch (Exception ex)
        {
            LogPurchaseFailed(productId, ex);
            return PurchaseResult.Failed;
        }
        finally
        {
            await CrossInAppBilling.Current.DisconnectAsync();
        }
    }

    /// <summary>
    /// Checks if a product has already been purchased.
    /// </summary>
    public async Task<bool> IsProductPurchasedAsync(string productId)
    {
        // First check local settings for remove ads
        if (productId == RemoveAdsProductId && settingsService.AdsRemoved)
        {
            return true;
        }

        try
        {
            var connected = await CrossInAppBilling.Current.ConnectAsync();
            if (!connected)
            {
                return false;
            }

            var purchases = await CrossInAppBilling.Current.GetPurchasesAsync(ItemType.InAppPurchase);
            return purchases?.Any(p => p.ProductId == productId) ?? false;
        }
        catch (Exception ex)
        {
            LogQueryPurchaseFailed(productId, ex);
            return false;
        }
        finally
        {
            await CrossInAppBilling.Current.DisconnectAsync();
        }
    }

    /// <summary>
    /// Restores previously made purchases.
    /// </summary>
    public async Task<bool> RestorePurchasesAsync()
    {
        try
        {
            var connected = await CrossInAppBilling.Current.ConnectAsync();
            if (!connected)
            {
                LogBillingConnectionFailed();
                return false;
            }

            var purchases = await CrossInAppBilling.Current.GetPurchasesAsync(ItemType.InAppPurchase);

            if (purchases == null)
            {
                return false;
            }

            var restored = false;
            foreach (var purchase in purchases)
            {
                if (purchase.ProductId == RemoveAdsProductId)
                {
                    adsService.DisableAds();
                    restored = true;
                    LogPurchaseRestored(purchase.ProductId);
                }
            }

            return restored;
        }
        catch (Exception ex)
        {
            LogRestorePurchasesFailed(ex);
            return false;
        }
        finally
        {
            await CrossInAppBilling.Current.DisconnectAsync();
        }
    }

    private PurchaseResult HandlePurchaseException(string productId, InAppBillingPurchaseException ex)
    {
        switch (ex.PurchaseError)
        {
            case PurchaseError.UserCancelled:
                LogPurchaseCancelled(productId);
                return PurchaseResult.Cancelled;

            case PurchaseError.AlreadyOwned:
                LogPurchaseAlreadyOwned(productId);
                if (productId == RemoveAdsProductId)
                {
                    adsService.DisableAds();
                }
                return PurchaseResult.AlreadyOwned;

            case PurchaseError.AppStoreUnavailable:
            case PurchaseError.BillingUnavailable:
                LogBillingUnavailable(productId);
                return PurchaseResult.NotSupported;

            default:
                LogPurchaseFailed(productId, ex);
                return PurchaseResult.Failed;
        }
    }

    [LoggerMessage(EventId = 1, Level = LogLevel.Information, Message = "In-app purchase service initialized")]
    partial void LogIapInitialized();

    [LoggerMessage(EventId = 2, Level = LogLevel.Error, Message = "Failed to initialize in-app purchase service")]
    partial void LogIapInitializationFailed(Exception ex);

    [LoggerMessage(EventId = 3, Level = LogLevel.Warning, Message = "Failed to connect to billing service")]
    partial void LogBillingConnectionFailed();

    [LoggerMessage(EventId = 4, Level = LogLevel.Error, Message = "Failed to get price for product: {ProductId}")]
    partial void LogGetPriceFailed(string productId, Exception ex);

    [LoggerMessage(EventId = 5, Level = LogLevel.Information, Message = "Purchase succeeded for product: {ProductId}")]
    partial void LogPurchaseSucceeded(string productId);

    [LoggerMessage(EventId = 6, Level = LogLevel.Information, Message = "Purchase cancelled for product: {ProductId}")]
    partial void LogPurchaseCancelled(string productId);

    [LoggerMessage(EventId = 7, Level = LogLevel.Information, Message = "Product already owned: {ProductId}")]
    partial void LogPurchaseAlreadyOwned(string productId);

    [LoggerMessage(EventId = 8, Level = LogLevel.Warning, Message = "Billing unavailable for product: {ProductId}")]
    partial void LogBillingUnavailable(string productId);

    [LoggerMessage(EventId = 9, Level = LogLevel.Error, Message = "Purchase failed for product: {ProductId}")]
    partial void LogPurchaseFailed(string productId, Exception ex);

    [LoggerMessage(EventId = 10, Level = LogLevel.Error, Message = "Failed to query purchase for product: {ProductId}")]
    partial void LogQueryPurchaseFailed(string productId, Exception ex);

    [LoggerMessage(EventId = 11, Level = LogLevel.Information, Message = "Purchase restored: {ProductId}")]
    partial void LogPurchaseRestored(string productId);

    [LoggerMessage(EventId = 12, Level = LogLevel.Error, Message = "Failed to restore purchases")]
    partial void LogRestorePurchasesFailed(Exception ex);
}
