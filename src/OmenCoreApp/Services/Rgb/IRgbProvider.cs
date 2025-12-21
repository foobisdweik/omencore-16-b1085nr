using System.Threading.Tasks;

namespace OmenCore.Services.Rgb
{
    /// <summary>
    /// Minimal interface for RGB providers (Corsair, Razer, Logitech)
    /// Implementations should be added in Services/Rgb and registered with RgbManager.
    /// </summary>
    public interface IRgbProvider
    {
        string ProviderName { get; }
        bool IsAvailable { get; }
        Task InitializeAsync();
        Task ApplyEffectAsync(string effectId);
    }
}