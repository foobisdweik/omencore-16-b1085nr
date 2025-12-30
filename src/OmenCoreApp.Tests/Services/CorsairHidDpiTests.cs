using System.Threading.Tasks;
using FluentAssertions;
using OmenCore.Services.Corsair;
using OmenCore.Services;
using Xunit;

namespace OmenCoreApp.Tests.Services
{
    public class CorsairHidDpiTests
    {
        private class TestCorsairHidDirect : CorsairHidDirect
        {
            public TestCorsairHidDirect(LoggingService logging) : base(logging) { }

            // Expose protected BuildSetDpiReport for testing via public wrapper
            public byte[]? CallBuildSetDpiReport(int pid, int index, int dpi)
            {
                // Add test device
                AddTestHidDevice("test", pid, OmenCore.Corsair.CorsairDeviceType.Mouse, "TestMouse");
                var hid = GetHidDeviceByDeviceId("test");
                if (hid == null) return null;
                return BuildSetDpiReport(hid, index, dpi);
            }
        }

        [Fact]
        public void BuildSetDpiReport_DarkCore_ProducesExpectedCommand()
        {
            var log = new LoggingService();
            var t = new TestCorsairHidDirect(log);

            var report = t.CallBuildSetDpiReport(0x1B2E, 0, 800);

            report.Should().NotBeNull();
            report![1].Should().Be(0x11);
            report[2].Should().Be((byte)0);
            report[3].Should().Be((byte)(800 & 0xFF));
            report[4].Should().Be((byte)((800 >> 8) & 0xFF));
        }

        [Fact]
        public void BuildSetDpiReport_M65_ProducesExpectedCommand()
        {
            var log = new LoggingService();
            var t = new TestCorsairHidDirect(log);

            var report = t.CallBuildSetDpiReport(0x1B1E, 1, 1600);

            report.Should().NotBeNull();
            report![1].Should().Be(0x12);
            report[2].Should().Be((byte)1);
            report[3].Should().Be((byte)(1600 & 0xFF));
            report[4].Should().Be((byte)((1600 >> 8) & 0xFF));
            report[5].Should().Be(0x01);
        }
    }
}
