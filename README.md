# JF Remote

A standalone mobile remote for Jellyfin, served by the server itself. Open
`{your-server}/JfRemote/` on a phone, sign in with your normal Jellyfin account,
and control **whatever is already playing** on any session — no casting, no app
installs. Works in any modern mobile browser (iOS and Android); use "Add to Home
Screen" for a fullscreen, app-like experience with its own icon.

## What it does

Pick any active session (the TV, another browser, a phone) and drive it:

| Area | Details |
|---|---|
| Playstate | play/pause, next/previous, ±30 s, stop |
| Seeking | drag the seek bar to scrub with **trickplay thumbnail previews**, release to seek |
| Segment skip | a "Skip intro ▸" button appears whenever the playhead is inside an intro/outro media segment (works with any segment provider, e.g. Intro Skipper) |
| Tracks | audio track, subtitle track (incl. Off), max stream quality, playback speed |
| Volume | slider + mute (reflects the controlled client's reported volume) |
| Episodes | swipeable strip centered on the playing episode, showing the **controlled user's** watched checkmarks and resume progress; tap any episode to play it |
| Watch-along | opt-in (👁 button): your phone plays a live-synced mirror of the controlled stream — same episode, same audio track, position-locked within ~1-3 s. Read-only: it never affects the controlled session and never reports watch progress. ⛶ for fullscreen, ✕ to stop |
| Messaging | 💬 sends a pop-up message to the controlled screen |

Session state updates live over the server WebSocket (REST polling fallback).
The page self-updates when the server has a newer build.

## Install

**Recommended — plugin repository:** Dashboard → Plugins → Repositories → **+** →

```
https://raw.githubusercontent.com/mhollier117/jellyfin-repo/main/manifest.json
```

then install **JF Remote** from the catalog and restart. Builds are published for
Jellyfin 12.0.x and 10.11.x; the catalog picks the right one automatically.

**Manual:** copy the release folder for your server version (from
[Releases](https://github.com/mhollier117/jellyfin-plugin-jfremote/releases))
into your Jellyfin `plugins` directory and restart.

Then open `http(s)://your-server/JfRemote/`.

## Configuration

**There is nothing to configure server-side** — the plugin has no settings page.
All state (your login, device identity) lives per-browser. Reverse-proxy
`BaseUrl` subpaths are handled automatically.

Two things that live in Jellyfin's own settings:

- **Controlling other users' sessions** requires the *"Allow remote control of
  other users"* policy on **your** account (Dashboard → Users → profile).
  Controlling your own sessions always works.
- The controlled client must be connected (an open web player tab, an active
  app session).

## Tips & troubleshooting

- **Nothing listed under sessions?** Only clients that report
  `SupportsRemoteControl` appear. Open/refresh the target player.
- **Commands do nothing on a web player?** Refresh that player's tab — a stale
  browser session can hold a dead WebSocket and go "deaf" to commands.
- **Watch-along quality/limits:** the phone stream is a separate transcode
  (capped ~12 Mbps); subtitles are not carried over.
- iOS home-screen apps keep separate login storage — you'll sign in once more
  after adding the icon.

## Build from source

```
dotnet build src/Jellyfin.Plugin.JfRemote -c Release -p:JellyfinVersion=12.0.0-rc2   # Jellyfin 12.0
dotnet build src/Jellyfin.Plugin.JfRemote -c Release -p:JellyfinVersion=10.11.0      # Jellyfin 10.11
```

The entire web UI is one self-contained file
(`src/Jellyfin.Plugin.JfRemote/web/remote.html`) embedded into the plugin at
build time — no build step, no external assets.
