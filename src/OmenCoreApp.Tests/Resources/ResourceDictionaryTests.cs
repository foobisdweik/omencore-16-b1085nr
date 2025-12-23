using System;
using System.IO;
using System.Reflection;
using Xunit;

namespace OmenCoreApp.Tests.Resources
{
    public class ResourceDictionaryTests
    {
        [Fact]
        public void ResourceDictionariesContainRequiredKeys()
        {
            // Find repository root by walking up until a 'src' directory is found
            var dir = AppContext.BaseDirectory;
            DirectoryInfo di = new DirectoryInfo(dir);
            while (di != null && !Directory.Exists(Path.Combine(di.FullName, "src")))
            {
                di = di.Parent;
            }

            Assert.True(di != null, "Could not locate repository root (missing 'src' folder)");

            var path = Path.Combine(di.FullName, "src", "OmenCoreApp", "Styles", "ModernStyles.xaml");
            Assert.True(File.Exists(path), $"Expected resource file not found: {path}");

            var content = File.ReadAllText(path);

            Assert.Contains("x:Key=\"AccentGreenBrush\"", content);
            Assert.Contains("x:Key=\"AccentBrush\"", content);
            Assert.Contains("x:Key=\"TextPrimaryBrush\"", content);
        }
    }
}
