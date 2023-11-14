#!/bin/bash

# Default parameters
steamcmdDir="steamcmd"  # since steamcmd is available globally
cs2Dir="/cs2"           # default installation directory

# You can override the defaults by passing arguments
# $1 is the first argument (steamcmdDir), $2 is the second argument (cs2Dir)
if [ ! -z "$1" ]; then
    steamcmdDir=$1
fi

if [ ! -z "$2" ]; then
    cs2Dir=$2
fi

# Running the commands
$steamcmdDir +force_install_dir $cs2Dir +login anonymous +app_update 730 validate +quit