function SurfCVars()
    SendToServerConsole("sv_cheats 1")
    SendToServerConsole("mp_solid_teammates 0")
    SendToServerConsole("mp_ct_default_secondary weapon_usp_silencer")
    SendToServerConsole("mp_t_default_secondary weapon_usp_silencer")
    SendToServerConsole("sv_holiday_mode 0")
    SendToServerConsole("sv_party_mode 0")
    SendToServerConsole("mp_respawn_on_death_ct 1")
    SendToServerConsole("mp_respawn_on_death_t 1")
    SendToServerConsole("mp_respawn_immunitytime -1")

    -- Dropping guns
    SendToServerConsole("mp_death_drop_c4 1")
    SendToServerConsole("mp_death_drop_defuser 1")
    SendToServerConsole("mp_death_drop_grenade 1")
    SendToServerConsole("mp_death_drop_gun 1")
    SendToServerConsole("mp_drop_knife_enable 1")
    SendToServerConsole("mp_weapons_allow_map_placed 1")
    SendToServerConsole("mp_disconnect_kills_players 0")

    -- Hide money
    SendToServerConsole("mp_playercashawards 0")
    SendToServerConsole("mp_teamcashawards 0")

    -- Surf & BHOP
    SendToServerConsole("sv_airaccelerate 150")
    SendToServerConsole("sv_enablebunnyhopping 1")
    SendToServerConsole("sv_autobunnyhopping 1")
    SendToServerConsole("sv_falldamage_scale 0")
    SendToServerConsole("sv_staminajumpcost 0")
    SendToServerConsole("sv_staminalandcost 0")

    -- chat commands
    SendToServerConsole("sv_full_alltalk 1")
    SendToServerConsole("sv_alltalk 1")
    SendToServerConsole("sv_allchat 1")
    SendToServerConsole("sv_spec_hear 1")

    -- Allow time for the cs2 vote map thing
    SendToServerConsole("mp_round_restart_delay 15")

    SendToServerConsole("mp_friendlyfire 0")
    SendToServerConsole("mp_roundtime 30")
    SendToServerConsole("mp_roundtime_defuse 30")
    SendToServerConsole("mp_roundtime_hostage 30")
    SendToServerConsole("mp_warmuptime_all_players_connected 0")
    SendToServerConsole("mp_freezetime 0")
    SendToServerConsole("mp_team_intro_time 0")
    SendToServerConsole("mp_warmup_end")
    SendToServerConsole("mp_warmuptime 0")
    SendToServerConsole("bot_quota 0")
    SendToServerConsole("mp_autoteambalance 0")
    SendToServerConsole("mp_limitteams 0")
    SendToServerConsole("mp_humanteam CT")
    SendToServerConsole("mp_spectators_max 64")
    SendToServerConsole("sv_cheats 0")
end
