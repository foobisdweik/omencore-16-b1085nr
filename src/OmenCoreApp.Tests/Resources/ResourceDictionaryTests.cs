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
            var dir = AppContext.BaseDirectory ?? throw new Exception("AppContext.BaseDirectory is null");
            DirectoryInfo di = new DirectoryInfo(dir);
            DirectoryInfo? repoRoot = null;
            while (di != null)
            {
                if (Directory.Exists(Path.Combine(di.FullName, "src")))
                {
                    repoRoot = di;
                    break;
                }
                di = di.Parent;
            }

            if (repoRoot == null) throw new Exception("Could not locate repository root (missing 'src' folder)");

            var rootPath = repoRoot.FullName;
            var path = Path.Combine(rootPath, "src", "OmenCoreApp", "Styles", "ModernStyles.xaml");
            Assert.True(File.Exists(path), $"Expected resource file not found: {path}");

            var content = File.ReadAllText(path);

            Assert.Contains("x:Key=\"AccentGreenBrush\"", content);
            Assert.Contains("x:Key=\"AccentBrush\"", content);
            Assert.Contains("x:Key=\"TextPrimaryBrush\"", content);
        }
    }
}
