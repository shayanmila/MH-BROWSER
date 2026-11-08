# MH Browser — Windows desktop preview

This separate .NET 8 Windows Forms + Microsoft Edge WebView2 application is a **new desktop target**, not a compiled form of the Android Java application.

## Available in desktop v0.1
- Multiple browser tabs with create/close and basic page titles.
- Address/search bar with HTTPS normalization and Google search.
- Back, Forward, Reload, Home, and new-window handling.
- Web pages rendered using installed Microsoft Edge WebView2 Runtime.

## Requirements
Windows 10/11 x64; .NET 8 Desktop Runtime (x64); Microsoft Edge WebView2 Runtime (installed on most current Windows systems). The workflow publishes a portable *framework-dependent* package, not a standalone runtime.

## Important differences
The Android app's native download service, yt-dlp/FFmpeg media extraction, privacy browser process, ad blocker, saved shortcuts, settings and Android WebView preferences have **not** been ported. Do not represent this preview as feature-complete or privacy-hardened. WebView2 stores browser data locally by default in its user-data directory; this desktop prototype has not undergone security/privacy review.

## Build
Run the manual GitHub Actions workflow **MH Browser Windows Desktop (Manual Only)**. Download the ZIP from the workflow's Artifacts section. Unzip and run `MHBrowser.exe`.

The build is manual-only; commits do not trigger Windows builds. The existing Android workflows are separate.
