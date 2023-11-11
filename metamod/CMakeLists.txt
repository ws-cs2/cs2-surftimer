cmake_minimum_required(VERSION 3.0)
project(wst)

include_directories(
        hl2sdk
        hl2sdk/common
        hl2sdk/game/shared
        hl2sdk/game/server
        hl2sdk/public
        hl2sdk/public/engine
        hl2sdk/public/mathlib
        hl2sdk/public/tier0
        hl2sdk/public/tier1
        hl2sdk/public/entity2
        hl2sdk/public/game/server
        hl2sdk/lib/public/win64
        metamod-source/core
        metamod-source/core/sourcehook
        vendor/funchook/include

)

add_executable(wst
        src/wst.cpp
        src/wst.h
        src/core/framework.h
        src/core/module.h
        src/core/virtual.h
        src/core/schemasystem.cpp
        src/core/schemasystem.h
        src/core/cbaseentity.h
        src/core/whereami.cpp
        src/core/whereami.h
        src/autoupdater.cpp
        src/autoupdater.h
        src/script_or_zone_file.cpp
        src/script_or_zone_file.h
        src/core/common.h
)

# Dummy target to integrate custom build script
add_custom_target(MetamodBuild ALL
        COMMAND PowerShell.exe -File ${CMAKE_SOURCE_DIR}/local_build.ps1
        WORKING_DIRECTORY ${CMAKE_SOURCE_DIR})



