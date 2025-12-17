using System;

namespace OmenCore.Hardware
{
    /// <summary>
    /// Interface for MSR (Model-Specific Register) access providers.
    /// Implemented by PawnIOMsrAccess (recommended, Secure Boot compatible) and
    /// WinRing0MsrAccess (legacy, requires Secure Boot disabled).
    /// </summary>
    public interface IMsrAccess : IDisposable
    {
        /// <summary>
        /// Check if MSR access is available.
        /// </summary>
        bool IsAvailable { get; }
        
        // ==========================================
        // Intel Voltage Offset (Undervolt)
        // ==========================================
        
        /// <summary>
        /// Apply a voltage offset to CPU cores (negative = undervolt).
        /// </summary>
        /// <param name="offsetMv">Offset in millivolts (typically -200 to 0)</param>
        void ApplyCoreVoltageOffset(int offsetMv);
        
        /// <summary>
        /// Apply a voltage offset to CPU cache (negative = undervolt).
        /// </summary>
        /// <param name="offsetMv">Offset in millivolts (typically -200 to 0)</param>
        void ApplyCacheVoltageOffset(int offsetMv);
        
        /// <summary>
        /// Read current core voltage offset.
        /// </summary>
        int ReadCoreVoltageOffset();
        
        /// <summary>
        /// Read current cache voltage offset.
        /// </summary>
        int ReadCacheVoltageOffset();
        
        // ==========================================
        // TCC Offset (Thermal Control Circuit)
        // ==========================================
        
        /// <summary>
        /// Read the current TCC offset (temperature limit reduction).
        /// Returns 0-63, where 0 = no limit, 63 = max 63°C below TjMax.
        /// </summary>
        int ReadTccOffset();
        
        /// <summary>
        /// Read the TjMax (maximum junction temperature).
        /// </summary>
        int ReadTjMax();
        
        /// <summary>
        /// Set the TCC offset to limit maximum CPU temperature.
        /// Offset of N means CPU will throttle at (TjMax - N)°C.
        /// </summary>
        /// <param name="offset">Offset in degrees (0-63)</param>
        void SetTccOffset(int offset);
        
        /// <summary>
        /// Get the effective temperature limit (TjMax - TCC offset).
        /// </summary>
        int GetEffectiveTempLimit();
    }
}
