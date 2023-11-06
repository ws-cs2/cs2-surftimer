# Will's CS2 SurfTimer

A experimental surf timer using the depricated Lua [VScript API](https://cs2.poggu.me/dumped-data/vscript-list).

The timer only works on [surf_beginner](https://steamcommunity.com/sharedfiles/filedetails/?id=3070321829&searchtext=surf_beginner)

## Servers

[KZG | CS2 Surf](https://join.kzg.gg/cs2-surf) - `103.212.224.45:27085`

## Discord

Need help, interested in running a dedicated server or want to make some improvments? https://discord.gg/Ms3AdWdH4X

## Commands

- wst_r: Restarts your timer and teleports you to the startzone
- wst_top: prints the top 10 times to chat
- wst_cp: Checkpoint (saveloc)
- wst_tele: Teleport to checkpoint (stops your timer)



## Installation

First will first need to [install Metamod](https://www.sourcemm.net/downloads.php?branch=dev). Then install the [Lua Unlocker](https://github.com/Source2ZE/LuaUnlocker) and optionally [Movement Unlocker](https://github.com/Source2ZE/MovementUnlocker).

Copy the `scripts` folder here to your `game/csgo` folder.

Then in 'cfg' if you are running on listen server create (or add to) 'listenserver.cfg'. If you are running a dedicated server then create (or add to) 'server.cfg'.

```
sv_cheats 1
script_reload_code wst
sv_cheats 0
```

To persist times in a dedicated server, you must run cs2 via the python launcher script provided (See below to learn why)

The plugin should work without using the launcher, however your times will not save.

To use the launcher

```
python scripts/wst_launcher.py <all of your normal parameters you would pass to cs2.exe>
```
```
Example:

C:/cs2/game/bin/win64/cs2.exe -dedicated +map de_dust2

becomes

python scripts/wst_launcher.py -dedicated +map de_dust2
```

## Saving & Loading Times

The Lua VScript API is a sandbox and provides no way (that I know of) to save data to disk, call web API's or use databases.

Therefore we have made a python 'launcher' script that can be used to run CS2 dedicated servers. This script reads output from the server console to accept commands passed from the Lua plugin. For now it just stores times in Valve's VDF KeyValues format as files.

Lua plugins can then read files in this KeyValue format.

Map times are saved in `scripts/wst_records/<map_name>.txt`

File format:
```
"Leaderboard"
{
    "version" "_1.0"
    "data"
    {
        "STEAM_0:1:123456"
        {
            "name" "Player One"
            "time" "60.00"
        }
        "STEAM_0:0:654321"
        {
            "name" "Player Two"
            "time" "120.00"
        }
    }
}
```

## Future of this plugin

The goal for this project is just to make something that works - VScript is depreciated and will probably stop working at some point!

## Video

[![CS2 SurfTimer](https://img.youtube.com/vi/gdIbHZaUJAQ/0.jpg)](https://www.youtube.com/watch?v=gdIbHZaUJAQ "CS2 SurfTimer")






