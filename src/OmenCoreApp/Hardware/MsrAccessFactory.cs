using System;
using OmenCore.Services;

namespace OmenCore.Hardware
{
    /// <summary>
    /// Factory for creating MSR access providers.
    /// Prefers PawnIO (Secure Boot compatible) over WinRing0 (legacy).
    /// </summary>
    public static class MsrAccessFactory
    {
        /// <summary>
        /// Active backend being used.
        /// </summary>
        public static MsrBackend ActiveBackend { get; private set; } = MsrBackend.None;
        
        /// <summary>
        /// Status message describing the current MSR access state.
        /// </summary>
        public static string StatusMessage { get; private set; } = "Not initialized";
        
        /// <summary>
        /// Create an MSR access provider. Tries PawnIO first, falls back to WinRing0.
        /// Returns null if no backend is available.
        /// </summary>
        public static IMsrAccess? Create(LoggingService? logging = null)
        {
            // Try PawnIO first (Secure Boot compatible, recommended)
            try
            {
                var pawnIO = new PawnIOMsrAccess();
                if (pawnIO.IsAvailable)
                {
                    ActiveBackend = MsrBackend.PawnIO;
                    StatusMessage = "PawnIO MSR access available (Secure Boot compatible)";
                    logging?.Info($"✓ {StatusMessage}");
                    return pawnIO;
                }
                pawnIO.Dispose();
            }
            catch (Exception ex)
            {
                logging?.Debug($"PawnIO MSR init failed: {ex.Message}");
            }
            
            // Fall back to WinRing0 (legacy, requires Secure Boot disabled)
            // NOTE: This is deprecated and will be removed in a future version
            try
            {
                var winRing0 = new WinRing0MsrAccess();
                if (winRing0.IsAvailable)
                {
                    ActiveBackend = MsrBackend.WinRing0;
                    StatusMessage = "WinRing0 MSR access available (legacy, consider PawnIO)";
                    logging?.Warn($"⚠️ {StatusMessage}");
                    return winRing0;
                }
                winRing0.Dispose();
            }
            catch (Exception ex)
            {
                logging?.Debug($"WinRing0 MSR init failed: {ex.Message}");
            }
            
            // No backend available
            ActiveBackend = MsrBackend.None;
            StatusMessage = "No MSR access available. Install PawnIO for undervolt/TCC features.";
            logging?.Info(StatusMessage);
            return null;
        }
        
        /// <summary>
        /// Check if any MSR backend is available without creating an instance.
        /// </summary>
        public static bool IsAnyBackendAvailable()
        {
            // Quick check for PawnIO
            try
            {
                using var pawnIO = new PawnIOMsrAccess();
                if (pawnIO.IsAvailable) return true;
            }
            catch { }
            
            // Quick check for WinRing0
            try
            {
                using var winRing0 = new WinRing0MsrAccess();
                if (winRing0.IsAvailable) return true;
            }
            catch { }
            
            return false;
        }
    }
    
    public enum MsrBackend
    {
        None,
        PawnIO,
        WinRing0
    }
}
