using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using OmenCore.Hardware;
using OmenCore.Models;
using OmenCore.Services;
using Xunit;

namespace OmenCoreApp.Tests.Services
{
    public class SettingsRestorationServiceTests
    {
        private class TestFanController : IFanController
        {
            public bool IsAvailable => true;
            public string Status => "Test";
            public string Backend => "Test";
            public string? LastAppliedPreset { get; private set; }

            public bool ApplyPreset(FanPreset preset)
            {
                LastAppliedPreset = preset?.Name;
                return true;
            }

            public bool ApplyCustomCurve(System.Collections.Generic.IEnumerable<FanCurvePoint> curve) => true;
            public bool SetFanSpeed(int percent) => true;
            public bool SetMaxFanSpeed(bool enabled) => true;
            public bool SetPerformanceMode(string modeName) => true;
            public bool RestoreAutoControl() => true;
            public System.Collections.Generic.IEnumerable<FanTelemetry> ReadFanSpeeds() => new System.Collections.Generic.List<FanTelemetry>();
            public void ApplyMaxCooling() { LastAppliedPreset = "Max"; }
            public void ApplyAutoMode() { LastAppliedPreset = "Auto"; }
            public void ApplyQuietMode() { LastAppliedPreset = "Quiet"; }
            public void Dispose() { }
        }

        [Fact]
        public async Task RestoreFanPreset_ShouldApplyMax_WhenSavedAsMax()
        {
            var logging = new LoggingService();
            logging.Initialize();

            var configService = new ConfigurationService();
            configService.Config.LastFanPresetName = "Max";

            var hwMonitor = new LibreHardwareMonitorImpl();
            var thermalProvider = new ThermalSensorProvider(hwMonitor);
            var controller = new TestFanController();
            var fanService = new FanService(controller, thermalProvider, logging, 1000);

            var svc = new SettingsRestorationService(logging, configService, null, fanService);
            var sequencer = new StartupSequencer(logging);
            svc.RegisterTasks(sequencer);

            await sequencer.ExecuteAsync(CancellationToken.None);

            svc.FanPresetRestored.Should().BeTrue();
            // Our TestFanController records Max via ApplyMaxCooling
            controller.LastAppliedPreset.Should().Be("Max");

            logging.Dispose();
        }

        [Fact]
        public async Task RestoreFanPreset_ShouldApplyCustomPreset_WhenPresentInConfig()
        {
            var logging = new LoggingService();
            logging.Initialize();

            var configService = new ConfigurationService();
            var custom = new FanPreset { Name = "MyCustom", Mode = FanMode.Manual };
            configService.Config.FanPresets.Add(custom);
            configService.Config.LastFanPresetName = "MyCustom";

            var hwMonitor = new LibreHardwareMonitorImpl();
            var thermalProvider = new ThermalSensorProvider(hwMonitor);
            var controller = new TestFanController();
            var fanService = new FanService(controller, thermalProvider, logging, 1000);

            var svc = new SettingsRestorationService(logging, configService, null, fanService);
            var sequencer = new StartupSequencer(logging);
            svc.RegisterTasks(sequencer);

            await sequencer.ExecuteAsync(CancellationToken.None);

            svc.FanPresetRestored.Should().BeTrue();
            controller.LastAppliedPreset.Should().Be("MyCustom");

            logging.Dispose();
        }

        [Fact]
        public void SaveCustomPreset_ShouldPersistAndApply()
        {
            var logging = new LoggingService();
            logging.Initialize();

            var configService = new ConfigurationService();
            var hwMonitor = new LibreHardwareMonitorImpl();
            var thermalProvider = new ThermalSensorProvider(hwMonitor);
            var controller = new TestFanController();
            var fanService = new FanService(controller, thermalProvider, logging, 1000);

            var vm = new OmenCore.ViewModels.FanControlViewModel(fanService, configService, logging);

            // Prepare a custom curve and name
            vm.CustomFanCurve.Clear();
            vm.CustomFanCurve.Add(new FanCurvePoint { TemperatureC = 40, FanPercent = 30 });
            vm.CustomFanCurve.Add(new FanCurvePoint { TemperatureC = 80, FanPercent = 80 });
            vm.CustomPresetName = "UnitTestPreset";

            vm.SaveCustomPresetCommand.Execute(null);

            // The preset should be present in the FanPresets list
            var saved = vm.FanPresets.FirstOrDefault(p => p.Name == "UnitTestPreset");
            saved.Should().NotBeNull();

            // It should also have been applied (TestFanController records last applied preset name)
            controller.LastAppliedPreset.Should().Be("UnitTestPreset");

            // Config should have LastFanPresetName set
            configService.Load().LastFanPresetName.Should().Be("UnitTestPreset");

            logging.Dispose();
        }

        [Fact]
        public async Task ForceReapplyFanPreset_ShouldApplyMax_WhenSavedAsMax()
        {
            var logging = new LoggingService();
            logging.Initialize();

            var configService = new ConfigurationService();
            configService.Config.LastFanPresetName = "Max";

            var hwMonitor = new LibreHardwareMonitorImpl();
            var thermalProvider = new ThermalSensorProvider(hwMonitor);
            var controller = new TestFanController();
            var fanService = new FanService(controller, thermalProvider, logging, 1000);

            var svc = new SettingsRestorationService(logging, configService, null, fanService);
            var result = await svc.ForceReapplyFanPresetAsync();

            result.Should().BeTrue();
            controller.LastAppliedPreset.Should().Be("Max");

            logging.Dispose();
        }

        [Fact]
        public void ReapplySavedPresetCommand_ShouldApplySavedMax()
        {
            var logging = new LoggingService();
            logging.Initialize();

            var configService = new ConfigurationService();
            configService.Config.LastFanPresetName = "Max";

            var hwMonitor = new LibreHardwareMonitorImpl();
            var thermalProvider = new ThermalSensorProvider(hwMonitor);
            var controller = new TestFanController();
            var fanService = new FanService(controller, thermalProvider, logging, 1000);

            var vm = new OmenCore.ViewModels.FanControlViewModel(fanService, configService, logging);

            vm.ReapplySavedPresetCommand.Execute(null);

            controller.LastAppliedPreset.Should().Be("Max");

            logging.Dispose();
        }
    }
}

