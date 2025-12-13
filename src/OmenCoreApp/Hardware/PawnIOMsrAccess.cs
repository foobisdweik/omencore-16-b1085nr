using System;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading;

namespace OmenCore.Hardware
{
    /// <summary>
    /// PawnIO-based MSR access provider for Secure Boot compatible systems.
    /// Uses the signed PawnIO driver with IntelMSR module.
    /// </summary>
    public sealed class PawnIOMsrAccess : IDisposable
    {
        private IntPtr _handle = IntPtr.Zero;
        private IntPtr _pawnIOLib = IntPtr.Zero;
        private bool _moduleLoaded;
        private bool _disposed;

        // Embedded IntelMSR module binary
        private static byte[]? _intelMsrModule;

        // Function delegates
        private delegate int PawnioOpen(out IntPtr handle);
        private delegate int PawnioLoad(IntPtr handle, byte[] blob, IntPtr size);
        private delegate int PawnioExecute(IntPtr handle, string name, ulong[] input, IntPtr inSize, ulong[] output, IntPtr outSize, out IntPtr returnSize);
        private delegate int PawnioClose(IntPtr handle);

        private PawnioOpen? _pawnioOpen;
        private PawnioLoad? _pawnioLoad;
        private PawnioExecute? _pawnioExecute;
        private PawnioClose? _pawnioClose;

        public bool IsAvailable => _handle != IntPtr.Zero && _moduleLoaded;

        public PawnIOMsrAccess()
        {
            Initialize();
        }

        private bool Initialize()
        {
            try
            {
                // Try bundled PawnIOLib.dll first
                string appDir = AppDomain.CurrentDomain.BaseDirectory;
                string bundledLibPath = Path.Combine(appDir, "drivers", "PawnIOLib.dll");
                string? libPath = null;
                
                if (File.Exists(bundledLibPath))
                {
                    libPath = bundledLibPath;
                }
                else
                {
                    // Fall back to PawnIO installation
                    string? pawnIOPath = FindPawnIOInstallation();
                    if (pawnIOPath != null)
                    {
                        string installedLibPath = Path.Combine(pawnIOPath, "PawnIOLib.dll");
                        if (File.Exists(installedLibPath))
                        {
                            libPath = installedLibPath;
                        }
                    }
                }
                
                if (libPath == null) return false;

                _pawnIOLib = NativeMethods.LoadLibrary(libPath);
                if (_pawnIOLib == IntPtr.Zero) return false;

                // Resolve functions
                if (!ResolveFunctions()) return false;

                // Open PawnIO handle
                int hr = _pawnioOpen!(out _handle);
                if (hr < 0 || _handle == IntPtr.Zero) return false;

                // Load IntelMSR module
                if (!LoadMsrModule())
                {
                    _pawnioClose!(_handle);
                    _handle = IntPtr.Zero;
                    return false;
                }

                _moduleLoaded = true;
                return true;
            }
            catch
            {
                return false;
            }
        }

        private string? FindPawnIOInstallation()
        {
            try
            {
                using var key = Microsoft.Win32.Registry.LocalMachine.OpenSubKey(
                    @"SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall\PawnIO");
                if (key != null)
                {
                    string? installLocation = key.GetValue("InstallLocation") as string;
                    if (!string.IsNullOrEmpty(installLocation) && Directory.Exists(installLocation))
                    {
                        return installLocation;
                    }
                }
            }
            catch { }

            string defaultPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), "PawnIO");
            if (Directory.Exists(defaultPath)) return defaultPath;

            return null;
        }

        private bool ResolveFunctions()
        {
            IntPtr openPtr = NativeMethods.GetProcAddress(_pawnIOLib, "pawnio_open");
            IntPtr loadPtr = NativeMethods.GetProcAddress(_pawnIOLib, "pawnio_load");
            IntPtr executePtr = NativeMethods.GetProcAddress(_pawnIOLib, "pawnio_execute");
            IntPtr closePtr = NativeMethods.GetProcAddress(_pawnIOLib, "pawnio_close");

            if (openPtr == IntPtr.Zero || loadPtr == IntPtr.Zero || 
                executePtr == IntPtr.Zero || closePtr == IntPtr.Zero)
            {
                return false;
            }

            _pawnioOpen = Marshal.GetDelegateForFunctionPointer<PawnioOpen>(openPtr);
            _pawnioLoad = Marshal.GetDelegateForFunctionPointer<PawnioLoad>(loadPtr);
            _pawnioExecute = Marshal.GetDelegateForFunctionPointer<PawnioExecute>(executePtr);
            _pawnioClose = Marshal.GetDelegateForFunctionPointer<PawnioClose>(closePtr);

            return true;
        }

        private bool LoadMsrModule()
        {
            try
            {
                string appDir = AppDomain.CurrentDomain.BaseDirectory;
                string[] moduleNames = { "IntelMSR.bin", "IntelMSR.amx" };
                
                foreach (var moduleName in moduleNames)
                {
                    string modulePath = Path.Combine(appDir, "drivers", moduleName);
                    if (File.Exists(modulePath))
                    {
                        _intelMsrModule = File.ReadAllBytes(modulePath);
                        break;
                    }
                }

                if (_intelMsrModule == null || _intelMsrModule.Length == 0)
                {
                    string? pawnIOPath = FindPawnIOInstallation();
                    if (pawnIOPath != null)
                    {
                        foreach (var moduleName in moduleNames)
                        {
                            string installedModule = Path.Combine(pawnIOPath, "modules", moduleName);
                            if (File.Exists(installedModule))
                            {
                                _intelMsrModule = File.ReadAllBytes(installedModule);
                                break;
                            }
                        }
                    }
                }

                if (_intelMsrModule == null || _intelMsrModule.Length == 0) return false;

                int hr = _pawnioLoad!(_handle, _intelMsrModule, (IntPtr)_intelMsrModule.Length);
                return hr >= 0;
            }
            catch
            {
                return false;
            }
        }

        public void ApplyCoreVoltageOffset(int offsetMv)
        {
            // MSR 0x150 - IA32_VOLTAGE_PLANE_0 (Core)
            WriteVoltageOffset(0x150, offsetMv);
        }

        public void ApplyCacheVoltageOffset(int offsetMv)
        {
            // MSR 0x152 - IA32_VOLTAGE_PLANE_2 (Cache)
            WriteVoltageOffset(0x152, offsetMv);
        }

        public int ReadCoreVoltageOffset()
        {
            return ReadVoltageOffset(0x150);
        }

        public int ReadCacheVoltageOffset()
        {
            return ReadVoltageOffset(0x152);
        }

        private void WriteVoltageOffset(uint msr, int offsetMv)
        {
            EnsureAvailable();
            
            // Convert mV to MSR format (same logic as WinRing0MsrAccess)
            // This logic should be shared, but for now duplicating to avoid refactoring everything
            long offsetVal = (long)Math.Round(offsetMv * 1.024);
            ulong value = 0x8000001100000000; // Write command + 1.024 scale
            
            if (offsetVal < 0)
            {
                value |= (ulong)((0xFFE00000 + offsetVal) & 0xFFF00000); // Negative
            }
            else
            {
                value |= (ulong)(offsetVal & 0xFFF00000); // Positive
            }

            WriteMsr(msr, value);
        }

        private int ReadVoltageOffset(uint msr)
        {
            EnsureAvailable();
            
            try
            {
                ulong value = ReadMsr(msr);
                long offsetUnits = (long)((value >> 21) & 0x7FF);
                
                // Sign extend from 11 bits
                if ((offsetUnits & 0x400) != 0)
                {
                    offsetUnits |= unchecked((long)0xFFFFFFFFFFFFF800);
                }
                
                return (int)(offsetUnits * 1.024);
            }
            catch
            {
                return 0;
            }
        }

        private ulong ReadMsr(uint index)
        {
            ulong[] input = { index };
            ulong[] output = new ulong[2]; // low, high
            IntPtr returnSize;

            // Assuming "ioctl_msr_read" takes index and returns low/high
            int hr = _pawnioExecute!(_handle, "ioctl_msr_read", input, (IntPtr)1, output, (IntPtr)2, out returnSize);
            if (hr < 0)
            {
                throw new InvalidOperationException($"PawnIO MSR read failed: HRESULT 0x{hr:X8}");
            }

            return output[0] | (output[1] << 32);
        }

        private void WriteMsr(uint index, ulong value)
        {
            ulong low = value & 0xFFFFFFFF;
            ulong high = value >> 32;
            ulong[] input = { index, low, high };
            ulong[] output = Array.Empty<ulong>();
            IntPtr returnSize;

            int hr = _pawnioExecute!(_handle, "ioctl_msr_write", input, (IntPtr)3, output, IntPtr.Zero, out returnSize);
            if (hr < 0)
            {
                throw new InvalidOperationException($"PawnIO MSR write failed: HRESULT 0x{hr:X8}");
            }
        }

        private void EnsureAvailable()
        {
            if (_disposed) throw new ObjectDisposedException(nameof(PawnIOMsrAccess));
            if (!IsAvailable) throw new InvalidOperationException("PawnIO MSR access is not available");
        }

        public void Dispose()
        {
            if (_disposed) return;
            _disposed = true;

            if (_handle != IntPtr.Zero && _pawnioClose != null)
            {
                _pawnioClose(_handle);
                _handle = IntPtr.Zero;
            }

            if (_pawnIOLib != IntPtr.Zero)
            {
                NativeMethods.FreeLibrary(_pawnIOLib);
                _pawnIOLib = IntPtr.Zero;
            }

            _moduleLoaded = false;
        }

        private static class NativeMethods
        {
            [DllImport("kernel32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
            public static extern IntPtr LoadLibrary(string lpFileName);

            [DllImport("kernel32.dll", SetLastError = true)]
            public static extern bool FreeLibrary(IntPtr hModule);

            [DllImport("kernel32.dll", SetLastError = true, CharSet = CharSet.Ansi)]
            public static extern IntPtr GetProcAddress(IntPtr hModule, string procName);
        }
    }
}
