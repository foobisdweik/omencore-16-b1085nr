using System.Threading.Tasks;

namespace OmenCore.Services.Rgb
{
    public class LogitechRgbProvider : IRgbProvider
    {
        public string ProviderName => "Logitech";
        public bool IsAvailable { get; private set; } = false;

        public Task InitializeAsync()
        {
            // TODO: Integrate G HUB SDK or HID fallback
            IsAvailable = false;
            return Task.CompletedTask;
        }

        public Task ApplyEffectAsync(string effectId)
        {
            // TODO: Implement effect application
            return Task.CompletedTask;
        }
    }
}