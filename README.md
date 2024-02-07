# Will's CS2 SurfTimer (DEPRECIATED)

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

- oce.surf AU | TURBO SURF (T1) au2.oce.surf:27015
- oce.surf AU | Mixed Maps (T1-T4) au2.oce.surf:27018
- oce.surf AU | VIP Surf au2.oce.surf:27016
- oce.surf EU | Mixed Maps (T1-T4) eu1.oce.surf:26709

## Discord

Need help, interested in running a dedicated server or want to make some improvments? https://discord.gg/Ms3AdWdH4X

## Commands

```
[Console] !r       / wst_r       - Teleport to the start zone
[Console] !top     / wst_top     - Show the top 10 players on this map
[Console] !wr      / wst_wr      - Show the server record
[Console] !sr      / wst_sr      - Show the server record
[Console] !pr      / wst_pr      - Show your personal record on this map
[Console] !cp      / wst_cp      - Save your current position
[Console] !tele    / wst_tele    - Teleport to your saved position (stops your timer)
[Console] !spec    / wst_spec    - Go to spectator team
[Console] !hidehud / wst_hidehud - Hide the hud
[Console] !showhud / wst_showhud - Show the hud
[Console] !getpos  / wst_getpos  - Shows your current x, y, z location (for zoning maps)
[Console] !help    / wst_help    - Shows the help menu
```


## Admin Commands

```
wst_mm_delete_top_records <mapname> <number_of_records> - Removes the top N records for a map (incase of glitched times), requires map reload to show in game
wst_mm_update scripts - Updates Lua scripts
wst_mm_update zones - Updates zones from github
```

## Installation

First will first need to [install Metamod](https://cs2.poggu.me/metamod/installation/). 

Then install [Will's SurfTimer Metamod Plugin](https://github.com/ws-cs2/cs2-surftimer/releases/) and [CS2Fixes](https://github.com/Source2ZE/CS2Fixes/releases/)

The first time you run the server run the following commands in your server console
```
wst_mm_update scripts
wst_mm_update zones
```

These commands will automatically download the latest Vscript's and zones for the timer. As the timer is being updated everyday I would recommend updating the scripts regularily.

In the 'cfg' folder if you are running on listen server create (or add to) 'listenserver.cfg'. 
If you are running a dedicated server then create (or add to) 'server.cfg'.
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






