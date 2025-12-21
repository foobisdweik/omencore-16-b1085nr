using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace OmenCore.Services.Rgb
{
    public class RgbManager
    {
        private readonly List<IRgbProvider> _providers = new();

        public IEnumerable<IRgbProvider> Providers => _providers;

        public void RegisterProvider(IRgbProvider provider)
        {
            if (!_providers.Contains(provider)) _providers.Add(provider);
        }

        public async Task InitializeAllAsync()
        {
            foreach (var p in _providers) await p.InitializeAsync();
        }

        public async Task ApplyEffectToAllAsync(string effectId)
        {
            var available = _providers.Where(p => p.IsAvailable);
            foreach (var p in available) await p.ApplyEffectAsync(effectId);
        }
    }
}