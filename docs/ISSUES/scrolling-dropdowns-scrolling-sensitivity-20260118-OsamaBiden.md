Title: Drop down section scrolling not working / inconsistent scroll sensitivity
Reporter: OsamaBiden
Reported at: 2026-01-18 18:36 (local)
Affected area: UI - All dropdown sections (notably the Optimizer tab)
App version: 2.5.0
OS: Windows (user reported)

Description
-----------
- Drop down sections (any control that expands/collapses or contains a dropdown/scrollable area) are not responding to scroll input in some cases — scrollbar does not move nor do items scroll.
- In other sections (example: Optimizer tab), the scroll sensitivity is extremely high (tiny wheel movement jumps the content a lot).
- Some sections are laggy and have low scroll sensitivity (slow response to wheel/trackpad), causing missed scrolls and frustrating UX.

Steps to reproduce
------------------
1. Open the app (v2.5.0) on Windows
2. Navigate to a page with a dropdown/expandable section (e.g., Optimizer tab, other dropdown UI controls)
3. Try to scroll the dropdown content using mouse wheel or trackpad
4. Observe either: no response, extremely sensitive jittery jumps, or slow/laggy scrolling

Expected behavior
-----------------
- All dropdowns should scroll smoothly with consistent sensitivity across the app.
- A small mouse wheel movement should scroll a small amount; larger movement should scroll proportionally.
- Scrolling should be responsive with low latency.

Actual behavior
---------------
- Some dropdowns do not scroll at all.
- Some dropdowns are overly sensitive and jump large distances with small scroll inputs.
- Some dropdowns are laggy with low sensitivity.

Additional notes / environment
----------------------------
- Issue reported by "OsamaBiden" at 18:36.
- Could be related to per-control ScrollViewer settings, input event handling, or differences in the Layout/Virtualization behavior between views.

Suggested investigation steps
-----------------------------
1. Reproduce locally with mouse wheel and trackpad across multiple views (Optimizer, Diagnostics, Settings, etc.).
2. Inspect XAML for dropdowns and scroll containers; check for explicit ScrollViewer settings (PanningMode, ScrollBarsVisibility, ManipulationMode)
3. Verify scroll event handling is not intercepted by ancestors (PreviewMouseWheel handlers that mark events handled)
4. Check for VirtualizingStackPanel or virtualization settings that could affect smoothness
5. Audit any custom scroll/gesture code or platform-specific behaviors (e.g., touch/precision touchpad settings)
6. Add telemetry/logging around mouse wheel events in problematic controls to capture delta/time between events when issue occurs

Priority / Severity: Medium (affects usability across several UI pages)

Attachments
-----------
- (Include screenshot or short screen recording showing the issue if available)

Reported by: OsamaBiden
Time: 2026-01-18 18:36

---

If you want, I can open a GitHub issue with this content and assign it to the UI/UX label and relevant maintainers. Let me know how you'd like to proceed.