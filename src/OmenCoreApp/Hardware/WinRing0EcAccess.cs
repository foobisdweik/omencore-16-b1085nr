using Microsoft.Win32.SafeHandles;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading;

namespace OmenCore.Hardware
{
    public sealed class WinRing0EcAccess : IEcAccess
    {
        private SafeFileHandle? _handle;
        private string _devicePath = string.Empty;
        private bool _disposed;

        /// <summary>
        /// Allowlist of EC addresses that are safe to write (fan control only).
        /// Prevents accidental writes to critical hardware registers like VRM control, battery charger, etc.
        /// IMPORTANT: Keyboard RGB EC addresses (0xB0-0xBE) are NOT included because
        /// they vary by model and can cause system crashes on some hardware (e.g., OMEN 17-ck2xxx).
        /// </summary>
        private static readonly HashSet<ushort> AllowedWriteAddresses = new()
        {
            // Fan control registers (HP Omen typical addresses - adjust for your hardware)
            0x2C, // Fan 1 set speed % (XSS1) - OmenMon-style, newer models
            0x2D, // Fan 2 set speed % (XSS2) - OmenMon-style, newer models
            0x2E, // Fan 1 speed % (legacy)
            0x2F, // Fan 2 speed % (legacy)
            0x44, // Fan 1 duty cycle
            0x45, // Fan 2 duty cycle
            0x46, // Fan control mode
            0x4A, // Fan 1 speed low byte
            0x4B, // Fan 1 speed high byte
            0x4C, // Fan 2 speed low byte
            0x4D, // Fan 2 speed high byte
            0xB0, // Fan speed target CPU
            0xB1, // Fan speed target GPU
            
            // Note: 0x6C (dust cleaning/fan reversal) is NOT included because true fan reversal
            // requires OMEN Max hardware with omnidirectional BLDC fans. Writing to this register
            // on unsupported hardware could be dangerous.
            
            // NOTE: Keyboard backlight EC addresses (0xB2-0xBE) are NOT safe to write!
            // These registers vary by model and caused hard crashes on OMEN 17-ck2xxx.
            // Use WMI BIOS SetColorTable() for keyboard lighting instead.
            
            // Performance modes
            0xCE, // Performance mode register
            0xCF, // Power limit control
        };

        public bool IsAvailable => _handle is { IsInvalid: false };

        public bool Initialize(string devicePath)
        {
            _devicePath = devicePath;
            _handle?.Dispose();
            _handle = Native.CreateFile(devicePath,
                Native.FILE_GENERIC_READ | Native.FILE_GENERIC_WRITE,
                Native.FILE_SHARE_READ | Native.FILE_SHARE_WRITE,
                IntPtr.Zero,
                Native.OPEN_EXISTING,
                0,
                IntPtr.Zero);
            return IsAvailable;
        }

        public byte ReadByte(ushort address)
        {
            EnsureHandle();
            // Read operations are generally safe, no allowlist needed
            var payload = new EcRegister { Address = address, Value = 0 };
            var ok = Native.DeviceIoControl(_handle!, Native.IOCTL_EC_READ,
                ref payload, Marshal.SizeOf<EcRegister>(),
                ref payload, Marshal.SizeOf<EcRegister>(),
                out _, IntPtr.Zero);
            if (!ok)
            {
                throw new Win32Exception(Marshal.GetLastWin32Error(), $"EC read failed at 0x{address:X4}");
            }
            return payload.Value;
        }

        public void WriteByte(ushort address, byte value)
        {
            EnsureHandle();
            
            // CRITICAL SAFETY CHECK: Only allow writes to pre-approved addresses
            if (!AllowedWriteAddresses.Contains(address))
            {
                var allowedList = string.Join(", ", AllowedWriteAddresses.Select(a => $"0x{a:X4}"));
                throw new UnauthorizedAccessException(
                    $"EC write to address 0x{address:X4} is blocked for safety. " +
                    $"Only approved addresses can be written to prevent hardware damage. " +
                    $"Allowed addresses: {allowedList}");
            }
            
            var payload = new EcRegister { Address = address, Value = value };
            var ok = Native.DeviceIoControl(_handle!, Native.IOCTL_EC_WRITE,
                ref payload, Marshal.SizeOf<EcRegister>(),
                IntPtr.Zero, 0, out _, IntPtr.Zero);
            if (!ok)
            {
                throw new Win32Exception(Marshal.GetLastWin32Error(), $"EC write failed at 0x{address:X4}");
            }
            Thread.Sleep(1);
        }

        private void EnsureHandle()
        {
            if (_disposed)
            {
                throw new ObjectDisposedException(nameof(WinRing0EcAccess));
            }
            if (!IsAvailable)
            {
                throw new InvalidOperationException($"EC bridge {_devicePath} is not ready");
            }
        }
        
        public void Dispose()
        {
            if (_disposed) return;
            _disposed = true;
            _handle?.Dispose();
            _handle = null;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct EcRegister
        {
            public ushort Address;
            public byte Value;
        }

        private static class Native
        {
            public const uint FILE_GENERIC_READ = 0x80000000;
            public const uint FILE_GENERIC_WRITE = 0x40000000;
            public const uint FILE_SHARE_READ = 0x00000001;
            public const uint FILE_SHARE_WRITE = 0x00000002;
            public const uint OPEN_EXISTING = 3;
            public const uint IOCTL_EC_READ = 0x80862007; // TODO replace with actual driver codes
            public const uint IOCTL_EC_WRITE = 0x8086200B;

            [DllImport("kernel32", SetLastError = true, CharSet = CharSet.Unicode)]
            public static extern SafeFileHandle CreateFile(
                string lpFileName,
                uint dwDesiredAccess,
                uint dwShareMode,
                IntPtr lpSecurityAttributes,
                uint dwCreationDisposition,
                uint dwFlagsAndAttributes,
                IntPtr hTemplateFile);

            [DllImport("kernel32", SetLastError = true)]
            public static extern bool DeviceIoControl(
                SafeFileHandle hDevice,
                uint dwIoControlCode,
                ref EcRegister inBuffer,
                int nInBufferSize,
                ref EcRegister outBuffer,
                int nOutBufferSize,
                out int bytesReturned,
                IntPtr overlapped);

            [DllImport("kernel32", SetLastError = true)]
            public static extern bool DeviceIoControl(
                SafeFileHandle hDevice,
                uint dwIoControlCode,
                ref EcRegister inBuffer,
                int nInBufferSize,
                IntPtr outBuffer,
                int nOutBufferSize,
                out int bytesReturned,
                IntPtr overlapped);
        }
    }
}
