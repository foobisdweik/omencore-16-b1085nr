# Reddit Comment for v2.6.1

---

**OmenCore v2.6.1** is out with some important fixes:

🔧 **Bug Fixes:**
- Fan Max mode from system tray now actually works (was silently applying Performance instead)
- Fixed thermal protection "yo-yo" on high-power laptops (i9/4090) - fans no longer drop to 30% while still hot
- Stuck temperature readings now recover automatically via WMI fallback
- Fixed crash when running Fan Diagnostics

✨ **New:**
- General tab now shows CPU/GPU load %, power draw in watts, and RAM usage
- System tray tooltip shows fan RPM and GPU power
- Checkmarks show active mode in Quick Access menus
- Installer pre-selects PawnIO driver (recommended for best compatibility)

📥 [Download from GitHub](https://github.com/theantipopau/omencore/releases/tag/v2.6.1)

Works with OMEN laptops & desktops. Free, open source, no OGH required.
