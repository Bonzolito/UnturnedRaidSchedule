# RaidSchedule

OpenMod plugin for Unturned that restricts raiding to configured time windows.

## Features
- Fixed weekly raid window (default: Friday 16:00 UK → Monday 00:00 UK)
- Blocks structure, barricade, and locked-empty-vehicle damage outside the window
- Server-wide announcements when window opens/closes
- Configurable pre-close warnings (default: 60/30/15 minutes before close)
- Per-player throttled feedback when raid attempts are blocked
- `/raidtime` command to check current status

## Installation
1. Drop `RaidSchedule.dll` and `TimeZoneConverter.dll` into your server's `OpenMod/plugins/` folder
2. Start the server once to generate the default config
3. Edit `OpenMod/plugins/RaidSchedule/config.yaml` to your schedule
4. Restart the server

## Building from source
Requires .NET SDK 8.0+

## Config
See `config.yaml` for all options. Key fields:
- `timezone` — IANA name (e.g. `Europe/London`, `America/New_York`)
- `schedule.windowStart` / `windowEnd` — day + 24h time
- `preCloseWarnings` — minutes before close to broadcast warnings