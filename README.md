# Will's CS2 SurfTimer

A experimental simple surf timer using the depricated Lua [VScript API](https://cs2.poggu.me/dumped-data/vscript-list) & Metamod.

![image](https://github.com/ws-cs2/cs2-surftimer/assets/149922947/f97e68af-94d2-4a7b-ad80-e24492a8191c)

## Features
 - Hud with Time, Speed and Rank
 - Simple leaderboard
 - Saving and loading times
 - Configurable Zones
 - Multiple Endzones in a map
 - Basic checkpoint/savloc

## Servers

 - FutureGN Surf - `139.99.145.57:27019`
 - [KZG | CS2 Surf](https://join.kzg.gg/cs2-surf) - `103.212.224.45:27085`
 - sk8 | EZ SURF - TIMER & RANKS - `101.100.143.138:27085`
 - 24/7 Surf Utopia - Skinchanger Knifechanger - `128.140.106.242:27021`

## Discord

Need help, interested in running a dedicated server or want to make some improvments? https://discord.gg/Ms3AdWdH4X

## Commands

- wst_help: displays a help menu
- wst_r: Restarts your timer and teleports you to the startzone
- wst_top: prints the top 10 times to console
- wst_cp: Checkpoint (saveloc)
- wst_tele: Teleport to checkpoint (stops your timer)



## Installation

First will first need to [install Metamod](https://www.sourcemm.net/downloads.php?branch=dev). 

Then install [Will's SurfTimer Metamod Plugin]() Metamod plugin,  [Lua Unlocker](https://github.com/Source2ZE/LuaUnlocker) and optionally [Movement Unlocker](https://github.com/Source2ZE/MovementUnlocker).

Now copy the Lua Vscript code from the 'lua' folder in this repository to `game/csgo`.

Your resulting directory should look like 'game/csgo/scripts/vscripts/wst.lua' (and other wst files) &  'game/csgo/scripts/wst_zones/surf_beginner.txt' (and other zones).

Then in 'cfg' if you are running on listen server create (or add to) 'listenserver.cfg'. If you are running a dedicated server then create (or add to) 'server.cfg'.

```
sv_cheats 1
script_reload_code wst
sv_cheats 0
```

## Saving & Loading Times

The Lua VScript API is a sandbox and provides no way (that I know of) to save data to disk, call web API's or use databases.

Therefore we have made a Metamod plugin which handles saving times to disk.

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

## Zones

Zone files go in `scripts/wst_zones/<mapname>.txt`

v1 & v2 are the position in world space of two opposite corners of a box. Come discuss in discord if you are zoning maps so we can add the zones back into this repo.

surf_beginner example:
```
"Zones"
{
    "version" "_1.0"
    "data"
    {
        "start"
        {
            "v1" "-448.150726, -47.964149, 320"
            "v2" "191.463989, 257.725311, 500"
        }
        "end"
        {
            "v1" "-4544.320312, 4868.677734, 3096"
            "v2" "-6078.667480, 5248.378418, 3296"
        }
    }
}
```


## Future of this plugin

The goal for this project is just to make something that works for the short term - VScript is depreciated and will probably stop working at some point!

## Video

[![CS2 SurfTimer](https://img.youtube.com/vi/gdIbHZaUJAQ/0.jpg)](https://www.youtube.com/watch?v=gdIbHZaUJAQ "CS2 SurfTimer")






