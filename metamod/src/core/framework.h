#ifndef WST_FRAMEWORK_H
#define WST_FRAMEWORK_H

#include <ISmmPlugin.h>
#include "igameevents.h"
#include "module.h"
#include "schemasystem.h"


#pragma once



class Framework {
public:
    static ICvar*& CVar() {
        static ICvar* wst_CVar = nullptr;
        return wst_CVar;
    }

    static IFileSystem*& FileSystem() {
        static IFileSystem* wst_FullFileSystem = nullptr;
        return wst_FullFileSystem;
    }

    static INetworkServerService*& NetworkServerService() {
        static INetworkServerService* wst_NetworkServerService = nullptr;
        return wst_NetworkServerService;
    }

    static ISource2Server*& Source2Server() {
        static ISource2Server* wst_Source2Server = nullptr;
        return wst_Source2Server;
    }

    static IVEngineServer2*& EngineServer() {
        static IVEngineServer2* wst_EngineServer = nullptr;
        return wst_EngineServer;
    }

    static IGameEventManager2*& GameEventManager() {
        static IGameEventManager2* wst_GameEventManager = nullptr;
        return wst_GameEventManager;
    }

    // ServerCModule
    static CModule ServerModule() {
        static CModule wst_ServerCModule(GAMEBIN, "server");
        return wst_ServerCModule;
    }

    static ISteamHTTP*& SteamHTTP() {
        static ISteamHTTP* wst_SteamHTTP = nullptr;
        return wst_SteamHTTP;
    }

    static CModule SchemaSystemModule() {
        static CModule wst_SchemaSystemCModule(ROOTBIN, "schemasystem");
        return wst_SchemaSystemCModule;
    }

    static CSchemaSystem*& SchemaSystem() {
        static CSchemaSystem* wst_SchemaSystem = nullptr;
        return wst_SchemaSystem;
    }

    // Disable the creation of this class
    Framework() = delete;
    Framework(const Framework&) = delete;
    Framework& operator=(const Framework&) = delete;
};


#endif //WST_FRAMEWORK_H
