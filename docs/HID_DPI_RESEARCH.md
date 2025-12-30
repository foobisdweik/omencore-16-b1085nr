# Corsair HID DPI & Macro Research

Status: In progress (2025-12-30)

Objective:
- Collect known HID report formats (or reverse-engineered sequences) for configuring mouse DPI stages and uploading macros for Corsair devices.
- Identify commonalities across product families to create a generalizable implementation.
- Prioritize commonly-used PIDs (mice first), but include notes for keyboards that provide macros via HID.

Sources:
- Community reverse-engineering notes (Reddit threads, GitHub repos such as `cschneider82/corsair-tools` (if available), `openrazer` and `pysigrok` captures).
- HID descriptors and Wireshark/sniffer traces where available.
- Official SDK docs for features where available (iCue SDK docs for reference only).

Findings so far:

1) General approach
- Corsair mice typically accept HID writes for lighting, and some also accept DPI table updates via vendor-defined reports. The commands vary by product generation.
- There is no single public spec; most information comes from community reverse-engineering and sniffed traffic while using iCUE.

2) Candidate mouse PIDs to target first
- 0x1B2E - Dark Core RGB
- 0x1B1E - M65 RGB Elite
- 0x1B96 - M65 RGB Ultra
- 0x1B34 - Ironclaw RGB
- 0x1B3F - Harpoon RGB PRO

3) Observed command patterns (hypothetical examples)
- Some devices use a 'write config block' command (e.g., cmd 0x0B) followed by an offset and length and payload. DPI tables are small sets of 2-4 16-bit values representing CPI stages.
- Others accept a dedicated 'set DPI' command with stage index and CPI value.

4) Macro upload
- Macro programming via HID is more complex and varies widely; some devices store macros in a format requiring sequences: begin macro, append actions, commit macro.
- Many Corsair devices expose macro APIs only through iCUE SDK rather than raw HID.

5) Safety
- DPI and macro updates can change user device behavior; implement safety checks and require explicit user confirmation in UI.
- Always provide a backup/read operation if device supports reading current DPI table.

Next steps (implementation plan):
1. Implement a generic `BuildSetDpiReport(device, stageIndex, dpi)` API that selects payloads based on device PID/type. Start with Dark Core (0x1B2E) and M65 family as test PIDs.
2. Add unit tests that validate report bytes for known PIDs using community-provided examples (or constructed stubs if real captures are not available).
3. Implement `ApplyDpiStagesAsync` in `CorsairHidDirect` that builds and sends the appropriate sequence (with retries and telemetry) and checks for expected HID responses where possible.
4. Add UI (mouse DPI editor) with safety warnings and a 'Restore defaults' option.

Notes:
- If we cannot find a reliable public HID format for a PID, implement a no-op with a friendly message and log telemetry failures; allow maintainers to add device-specific payloads later.
- Consider adding a `CorsairHidPayloadOverrides.json` to allow maintainers (or power users) to add per-PID payload templates without rebuilding.

I'll begin step 1 by drafting `BuildSetDpiReport` and unit tests for `0x1B2E` and `0x1B1E` using conservative, reversible payloads where possible. If you have captured HID traces or an iCUE export for these mice, please attach them to speed up accurate payload crafting.