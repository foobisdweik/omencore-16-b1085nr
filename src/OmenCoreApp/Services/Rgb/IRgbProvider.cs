using System.Collections.Generic;
using System.Drawing;
using System.Threading.Tasks;

namespace OmenCore.Services.Rgb
{
    /// <summary>
    /// Interface for RGB providers (Corsair, Razer, Logitech, HP Keyboard).
    /// Implementations should be added in Services/Rgb and registered with RgbManager.
    /// </summary>
    public interface IRgbProvider
    {
        /// <summary>
        /// Display name of the provider (e.g., "Corsair", "Razer").
        /// </summary>
        string ProviderName { get; }
        
        /// <summary>
        /// Unique identifier for the provider.
        /// </summary>
        string ProviderId { get; }
        
        /// <summary>
        /// Whether the provider is initialized and has available devices.
        /// </summary>
        bool IsAvailable { get; }
        
        /// <summary>
        /// Whether the provider is currently connected to devices.
        /// </summary>
        bool IsConnected { get; }
        
        /// <summary>
        /// Number of connected devices.
        /// </summary>
        int DeviceCount { get; }
        
        /// <summary>
        /// List of supported effect types.
        /// </summary>
        IReadOnlyList<RgbEffectType> SupportedEffects { get; }
        
        /// <summary>
        /// Initialize the provider and discover devices.
        /// </summary>
        Task InitializeAsync();
        
        /// <summary>
        /// Apply an effect using an effect ID string (legacy support).
        /// Format: "color:#FF0000" or "preset:MyPreset" or "effect:breathing"
        /// </summary>
        Task ApplyEffectAsync(string effectId);
        
        /// <summary>
        /// Apply a static color to all devices.
        /// </summary>
        Task SetStaticColorAsync(Color color);
        
        /// <summary>
        /// Apply a breathing effect with the specified color.
        /// </summary>
        Task SetBreathingEffectAsync(Color color);
        
        /// <summary>
        /// Apply a spectrum/rainbow cycling effect.
        /// </summary>
        Task SetSpectrumEffectAsync();
        
        /// <summary>
        /// Turn off all RGB lighting.
        /// </summary>
        Task TurnOffAsync();
    }
    
    /// <summary>
    /// Supported RGB effect types.
    /// </summary>
    public enum RgbEffectType
    {
        Static,
        Breathing,
        Spectrum,
        Wave,
        Reactive,
        Custom,
        Off
    }
}