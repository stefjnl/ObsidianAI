using Microsoft.JSInterop;

namespace ObsidianAI.Web.Services;

/// <summary>
/// Service for handling vault browser resize operations via JavaScript interop
/// </summary>
public interface IVaultResizeService
{
    /// <summary>
    /// Initialize resize functionality for a panel
    /// </summary>
    ValueTask InitializeResizeAsync(
        string handleSelector,
        string panelSelector,
        double minWidth,
        double maxWidth,
        DotNetObjectReference<VaultResizeCallback> callback);

    /// <summary>
    /// Save panel width to browser storage
    /// </summary>
    ValueTask SaveWidthAsync(string key, double width);

    /// <summary>
    /// Load panel width from browser storage
    /// </summary>
    ValueTask<double> LoadWidthAsync(string key, double defaultWidth);
}

/// <summary>
/// Implementation of vault resize service using JavaScript interop
/// </summary>
public class VaultResizeService : IVaultResizeService
{
    private readonly IJSRuntime _jsRuntime;

    public VaultResizeService(IJSRuntime jsRuntime)
    {
        _jsRuntime = jsRuntime;
    }

    public async ValueTask InitializeResizeAsync(
        string handleSelector,
        string panelSelector,
        double minWidth,
        double maxWidth,
        DotNetObjectReference<VaultResizeCallback> callback)
    {
        try
        {
            await _jsRuntime.InvokeVoidAsync(
                "VaultResizeHelper.initialize",
                handleSelector,
                panelSelector,
                minWidth,
                maxWidth,
                callback);
        }
        catch (Exception ex)
        {
            // Log error but don't throw - resize functionality is not critical
            Console.WriteLine($"Failed to initialize resize: {ex.Message}");
        }
    }

    public async ValueTask SaveWidthAsync(string key, double width)
    {
        try
        {
            await _jsRuntime.InvokeVoidAsync("VaultResizeHelper.saveWidth", key, width);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Failed to save width: {ex.Message}");
        }
    }

    public async ValueTask<double> LoadWidthAsync(string key, double defaultWidth)
    {
        try
        {
            return await _jsRuntime.InvokeAsync<double>("VaultResizeHelper.loadWidth", key, defaultWidth);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Failed to load width: {ex.Message}");
            return defaultWidth;
        }
    }
}

/// <summary>
/// Callback class for handling resize events from JavaScript
/// </summary>
public class VaultResizeCallback
{
    private readonly Action<double> _onResize;

    public VaultResizeCallback(Action<double> onResize)
    {
        _onResize = onResize;
    }

    [JSInvokable]
    public void OnResize(double width)
    {
        _onResize?.Invoke(width);
    }
}
