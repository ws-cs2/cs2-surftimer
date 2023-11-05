# Will's CS2 SurfTimer

A experimental surf timer written in 1 day using the depricated Lua [VScript API](https://cs2.poggu.me/dumped-data/vscript-list).

This should work with multiple people on a server, however I haven't tested that. This is just a v0 and is likely very broken.

Times are NOT persistant across map loads AND the timer only works on [surf_beginner](https://steamcommunity.com/sharedfiles/filedetails/?id=3070321829&searchtext=surf_beginner)

## Installation

First will first need to [install Metamod](https://www.sourcemm.net/downloads.php?branch=dev). Then install the [Lua Unlocker](https://github.com/Source2ZE/LuaUnlocker) and optionally [Movement Unlocker](https://github.com/Source2ZE/MovementUnlocker).

Copy the `scripts` folder here to your `game/csgo` folder.

Then in 'cfg' if you are running on listen server create (or add to) 'listenserver.cfg'. If you are running a dedicated server then create (or add to) 'server.cfg'.

```
sv_cheats 1
script_reload_code wst
sv_cheats 0
```

## Commands

- wsr_r: Restarts your timer and teleports you to the startzone
- wst_top: prints the top 10 times to chat
- wst_cp: Checkpoint (saveloc)
- wst_tele: Teleport to checkpoint (stops your timer)

## Video


[![CS2 SurfTimer](https://img.youtube.com/vi/gdIbHZaUJAQ/0.jpg)](https://www.youtube.com/watch?v=gdIbHZaUJAQ "CS2 SurfTimer")


