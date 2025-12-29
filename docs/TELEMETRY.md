# Telemetry (Anonymous, Opt-in)

OmenCore includes a small, opt-in telemetry feature that collects anonymous, aggregated counts of how often direct HID writes to Corsair devices succeed or fail for each product ID (PID).

Key points:

- **Opt-in only:** The telemetry toggle is available in Settings and is disabled by default.
- **What is collected:** For each Corsair PID, a count of successful HID write attempts and failed attempts, plus a timestamp of last observation. No serial numbers, MAC addresses, or any device-identifying information is collected.
- **Storage:** The telemetry file is stored locally at `%LOCALAPPDATA%\OmenCore\telemetry.json` when telemetry is enabled and reports are recorded. You can delete this file at any time to remove collected data.
- **Privacy:** Aggregated PID counts are intended to help improve compatibility by identifying PIDs that frequently fail under HID-only mode so we can add PIDs or adjust payloads.
- **Opt-out:** Turning telemetry off prevents future collection; existing local telemetry file is retained until you delete it manually.

If you want to contribute telemetry data to the project (e.g., to help us gather more samples), open an issue and we can propose a privacy-safe upload approach with explicit consent and a short data retention policy.