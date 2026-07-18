# JF Remote

A standalone mobile remote for Jellyfin, served by the server itself at
`{your-server}/JfRemote/`. Open it on a phone, sign in with your normal Jellyfin
account, and control **whatever is already playing** on any session — no casting,
no app installs, works on iOS and Android browsers (add to home screen for an
app-like experience).

## Features

- **Session picker** — choose any controllable session; auto-selects whatever is playing
- **Full playstate control** — play/pause, next/previous, ±30s, stop, volume slider, mute
- **Trickplay scrubbing** — drag the seek bar to preview thumbnails, release to seek
- **Media-segment skip** — a "Skip intro ▸" button appears when the playhead is inside
  an intro/outro segment (works with any segment provider, e.g. Intro Skipper)
- **Audio / subtitle / quality / speed** — bottom-sheet pickers mirroring the web OSD
- **Episode strip** — swipeable episode browser centered on the playing episode, with
  the *controlled user's* watched checkmarks and resume progress; tap to play
- **Watch-along** — opt-in synced mirror of the controlled stream on your phone
  (same episode, same audio track, position-locked; read-only, never affects the
  controlled session and never reports watch progress)
- **Send a message** to the controlled screen
- Live updates over the server WebSocket with REST polling fallback
- Reverse-proxy `BaseUrl` subpaths supported; self-updating page cache

## Install

Copy the release folder for your server version into your Jellyfin `plugins`
directory and restart:

| Jellyfin | Build |
|---|---|
| 12.0.x | `net10.0` build (`targetAbi 12.0.0.0`) |
| 10.11.x | `net9.0` build (`targetAbi 10.11.0.0`) |

Then open `http(s)://your-server/JfRemote/`.

## Permissions

Controlling your own sessions always works. Controlling **other users'** sessions
requires the "Allow remote control of other users" policy on your account
(Dashboard → Users → profile).

## Build from source

```
dotnet build src/Jellyfin.Plugin.JfRemote -c Release -p:JellyfinVersion=12.0.0-rc2   # Jellyfin 12.0
dotnet build src/Jellyfin.Plugin.JfRemote -c Release -p:JellyfinVersion=10.11.0      # Jellyfin 10.11
```

The web UI is a single self-contained file (`src/Jellyfin.Plugin.JfRemote/web/remote.html`)
embedded into the plugin at build time.
