// Hack to get my IDE to work remove this before compiling
// #define COMPILER_MSVC

#include "wst.h"

#include <iserver.h>
#include <dbg.h>
#include <strtools.h>
#include <funchook.h>

#include <filesystem>
#include "core/whereami.h"
#include "core/framework.h"
#include "core/module.h"
#include "core/virtual.h"
#include "core/cbaseentity.h"
#include "autoupdater.h"
#include "entitysystem.h"

#include <steam/steam_gameserver.h>
#include <irecipientfilter.h>


namespace fs = std::filesystem;


void Message(const char *msg, ...) {
    va_list args;
    va_start(args, msg);

    char buf[1024] = {};
    V_vsnprintf(buf, sizeof(buf) - 1, msg, args);

    ConColorMsg(Color(255, 0, 255, 255), "[WST_MM] %s", buf);

    va_end(args);
}

// #define WST_DEBUG

void DEBUG_Message(const char *msg, ...) {
#ifdef WST_DEBUG
    va_list args;
    va_start(args, msg);
    Message(msg, args);
    va_end(args);
#endif
}

AutoUpdater g_AutoUpdater{};
WSTPlugin g_WSTPlugin;
PLUGIN_EXPOSE(WSTPlugin, g_WSTPlugin);

SH_DECL_HOOK0_void(IServerGameDLL, GameServerSteamAPIActivated, SH_NOATTRIB, 0);

SH_DECL_HOOK0_void(IServerGameDLL, GameServerSteamAPIDeactivated, SH_NOATTRIB, 0);

CON_COMMAND_F(wst_mm_clean, "Removes scripts", FCVAR_GAMEDLL | FCVAR_HIDDEN) {
    // wst_mm_clean
    // wst_mm_clean <scripts>

    const char *arg = args.Arg(1);
    if (strcmp(arg, "scripts") == 0) {
        Message("Removing scripts\n");
        g_AutoUpdater.cleanScripts();
        return;
    }
    Message("Invalid argument: %s\n", arg);
    Message("Usage: wst_mm_clean <scripts>\n");
}

CSteamGameServerAPIContext g_steamAPI;

CON_COMMAND_F(wst_mm_update, "Updates the lua code and/or zones files from github", FCVAR_GAMEDLL | FCVAR_HIDDEN) {
    // wst_mm_update
    // wst_mm_update <scripts|zones>
    if (!g_steamAPI.SteamHTTP()) {
        Message("Steam API not initialized\n");
        return;
    }

    const char *arg = args.Arg(1);
//
//    if (arg == nullptr || strcmp(arg, "all") == 0) {
//        Message("Updating all files\n");
//        g_AutoUpdater.updateLuaScript();
//        g_AutoUpdater.updateZoneFiles();
//        return;
//    }

    if (strcmp(arg, "scripts") == 0) {
        Message("Updating scripts\n");
        g_AutoUpdater.updateLuaScript();
        return;
    }

    if (strcmp(arg, "zones") == 0) {
        Message("Updating zones\n");
        g_AutoUpdater.updateZoneFiles();
        return;
    }

    Message("Invalid argument: %s\n", arg);
    Message("Usage: wst_mm_update <scripts|zones>\n");
}

// Called by wst lua plugin to save records to disk
// This should only be called by the sever console
CON_COMMAND_F(wst_mm_save_record, "Save a record to disk", FCVAR_GAMEDLL | FCVAR_HIDDEN) {
    // wst_mm_save_record <map_name> <steam_id> <time> <player name in quotes>
    // wst_mm_save_record surf_beginner "STEAM_0:1:123456789" 50 "player name"
    if (args.ArgC() != 5) {
        Message("Usage: wst_mm_save_record <map_name> <steam_id> <time> <player name in quotes>\n");
        // print args
        for (int i = 0; i < args.ArgC(); i++) {
            Message("Arg %d: [%s]\n", i, args.Arg(i));
        }
        return;
    }

    const char *map_name = args.Arg(1);
    const char *steam_id = args.Arg(2);
    float time = atof(args.Arg(3)); // Convert string to float
    const char *player_name = args.Arg(4);

    // Load or initialize our KeyValuesW
    KeyValues *kv = new KeyValues(map_name);
    KeyValues::AutoDelete autoDelete(kv); // Ensure automatic deletion on scope exit

    char filePath[512];
    Q_snprintf(filePath, sizeof(filePath), "scripts/wst_records/%s.txt", map_name);


    KeyValues *data;
    if (kv->LoadFromFile(Framework::FileSystem(), filePath, "MOD")) {
        // Find the data subkey
        data = kv->FindKey("data", true);
    } else {
        // Initialize the KeyValues structure if the file doesn't exist
        kv->SetString("version", "_1.0");
        data = kv->FindKey("data", true); // Create "data" if it doesn't exist
        DEBUG_Message("INITIALIZED NEW DATA STRUCTURE\n");
    }

    // Find or create the subkey for this record
    KeyValues *playerData = data->FindKey(steam_id, true);

    // Check if the new time is better than the existing time, or if it's a new record
    const char *existingTimeStr = playerData->GetString("time", nullptr);
    if (existingTimeStr == nullptr || time < atof(existingTimeStr)) {
        // Update the record
        playerData->SetFloat("time", time);
        playerData->SetString("name", player_name);
        if (kv->SaveToFile(Framework::FileSystem(), filePath, "MOD")) {
            Message("Record saved successfully for %s on %s [%f]\n", player_name, map_name, time);
        } else {
            Message("Failed to save record for %s on %s [%f]\n", player_name, map_name, time);
        }
    } else {
        Message("Record not saved for %s on %s [%f] because it's not better than the existing record [%s]\n",
                player_name, map_name, time, existingTimeStr);
    }
}

typedef void (*ClientPrint)(CBaseEntity *, int , const char *, ...);
static ClientPrint ClientPrintFn = nullptr;

CEntitySystem *GetEntitySystem()
{
#ifdef WIN32
    static int offset = 88;
#else
    static int offset = 80;
#endif
    return *reinterpret_cast<CGameEntitySystem**>((uintptr_t)(Framework::GameResourceService()) + offset);
}

#define HUD_PRINTNOTIFY		1
#define HUD_PRINTCONSOLE	2
#define HUD_PRINTTALK		3
#define HUD_PRINTCENTER		4


void SendChatToClient(int index, const char *msg, ...)
{
    va_list args;
    va_start(args, msg);
    char buf[256];
    V_vsnprintf(buf, sizeof(buf), msg, args);

    CEntitySystem* system = GetEntitySystem();
    if (!system)
        return;

    CBaseEntity* player = system->GetBaseEntity(CEntityIndex(index + 1));

    if (!player)
        return;

    ClientPrintFn(player, HUD_PRINTTALK, buf, nullptr, nullptr, nullptr, nullptr);
}

CON_COMMAND_F(wst_mm_chat, "Chat to client", FCVAR_GAMEDLL | FCVAR_HIDDEN) {
    // wst_mm_chat <client index> <message>
    if (args.ArgC() < 3) {
        Message("Usage: wst_mm_chat <client index> <message>\n");
        return;
    }

    int clientIndex = atoi(args.Arg(1));
    const char *message = args.Arg(2);

    // add \x10 to the start of the message to make it green
    char buf[256];
    V_snprintf(buf, sizeof(buf), "\x10%s", message);
    message = buf;


    SendChatToClient(clientIndex, message);
}

typedef void (*HostSay)(CBaseEntity * , CCommand & , bool, int,
const char*);
static HostSay m_pHostSay = nullptr;

void DetourHostSay(SC_CBaseEntity *pController, CCommand &args, bool teamonly, int unk1,
                   const char *unk2) {
    IGameEvent *pEvent = Framework::GameEventManager()->CreateEvent("player_chat", true);

    if (pEvent) {
        pEvent->SetBool("teamonly", teamonly);
        pEvent->SetInt("userid", pController->m_pEntity->GetEntityIndex().Get() - 1);
        pEvent->SetString("text", args[1]);

        Framework::GameEventManager()->FireEvent(pEvent, true);
    }

    // if the text starts with ! or / then we don't call m_pHostSay
    // check args length to prevent crash
    if (*args[1] == '/' || *args[1] == '!') {
        return;
    }

    m_pHostSay(pController, args, teamonly, unk1, unk2);
}



void WSTPlugin::Hook_GameServerSteamAPIActivated() {
    Message("Steam API Activated\n");

    g_steamAPI.Init();
    Framework::SteamHTTP() = g_steamAPI.SteamHTTP();
}

void WSTPlugin::Hook_GameServerSteamAPIDeactivated() {
    Message("Steam API Deactivated\n");
}


WSTConfig WSTPlugin::LoadOrCreateConfig() {
    // Load or initialize our KeyValuesW
    KeyValues *kv = new KeyValues("Config");
    KeyValues::AutoDelete autoDelete(kv); // Ensure automatic deletion on scope exit

    char filePath[512];
    Q_snprintf(filePath, sizeof(filePath), "scripts/wst_config/%s.txt", "config");

    bool detourHostSay = true;
    if (kv->LoadFromFile(Framework::FileSystem(), filePath, "MOD")) {
        // Find the data subkey
        detourHostSay = kv->GetBool("DetourHostSay", true);
    } else {
        // Initialize the KeyValues structure if the file doesn't exist
        kv->SetBool("DetourHostSay", true);
    }

    if (kv->SaveToFile(Framework::FileSystem(), filePath, "MOD")) {
        Message("Config saved successfully\n");
    } else {
        Message("Failed to save config\n");
    }

    WSTConfig config{};
    config.detourHostSay = detourHostSay;
    return config;
}

bool WSTPlugin::Load(PluginId id, ISmmAPI *ismm, char *error, size_t maxlen, bool late) {
    PLUGIN_SAVEVARS();

    Message("Plugin Loaded\n");
    g_AutoUpdater.ensureDirectoriesExist();

    GET_V_IFACE_ANY(GetFileSystemFactory, Framework::FileSystem(), IFileSystem, FILESYSTEM_INTERFACE_VERSION);
    GET_V_IFACE_CURRENT(GetEngineFactory, Framework::CVar(), ICvar, CVAR_INTERFACE_VERSION);
    GET_V_IFACE_ANY(GetEngineFactory, Framework::NetworkServerService(), INetworkServerService,
                    NETWORKSERVERSERVICE_INTERFACE_VERSION);
    GET_V_IFACE_ANY(GetServerFactory, Framework::Source2Server(), ISource2Server, SOURCE2SERVER_INTERFACE_VERSION);
    GET_V_IFACE_CURRENT(GetEngineFactory, Framework::EngineServer(), IVEngineServer2, INTERFACEVERSION_VENGINESERVER);
    GET_V_IFACE_CURRENT(GetEngineFactory, Framework::GameResourceService(), IGameResourceServiceServer, GAMERESOURCESERVICESERVER_INTERFACE_VERSION);

    Framework::GameEventManager() = (IGameEventManager2 *) (CALL_VIRTUAL(uintptr_t, 91, Framework::Source2Server()) -
                                                            8);

    SH_ADD_HOOK_MEMFUNC(IServerGameDLL, GameServerSteamAPIActivated, Framework::Source2Server(), this,
                        &WSTPlugin::Hook_GameServerSteamAPIActivated, false);
    SH_ADD_HOOK_MEMFUNC(IServerGameDLL, GameServerSteamAPIDeactivated, Framework::Source2Server(), this,
                        &WSTPlugin::Hook_GameServerSteamAPIDeactivated, false);


    WSTConfig config = LoadOrCreateConfig();

    // please do not break other metamod plugins :pray: :pray: :pray:
#ifdef _WIN32
    ClientPrintFn = (ClientPrint) Framework::ServerModule().FindSignature(
            R"(\x48\x85\xC9\x0F\x84\x2A\x2A\x2A\x2A\x48\x8B\xC4\x48\x89\x58\x18)");
#else
    ClientPrintFn = (ClientPrint)Framework::ServerModule().FindSignature(R"(\x55\x48\x89\xE5\x41\x57\x49\x89\xCF\x41\x56\x49\x89\xD6\x41\x55\x41\x89\xF5\x41\x54\x4C\x8D\xA5\xA0\xFE\xFF\xFF)");
#endif

    if (config.detourHostSay) {
        Message("Detouring HostSay\n");
        Message("If you are using MetaMod plugins such as CounterStrikeSharp/CS2Fixes, there is a good chance this will break them\n");
        // https://github.com/Source2ZE/CS2Fixes/blob/main/gamedata/cs2fixes.games.txt
        // For now only detour HostSay on windows, for linux tell people to use CounterStrikeSharp
        Framework::SchemaSystem() = (CSchemaSystem *) Framework::SchemaSystemModule().FindInterface(
                SCHEMASYSTEM_INTERFACE_VERSION);
#ifdef _WIN32
        // None of this code will work with CounterStrikeSharp
        m_pHostSay = (HostSay) Framework::ServerModule().FindSignature(
                R"(\x44\x89\x4C\x24\x2A\x44\x88\x44\x24\x2A\x55\x53\x56\x57\x41\x54\x41\x55)");
#else
        m_pHostSay = (HostSay)Framework::ServerModule().FindSignature(R"(\x55\x48\x89\xE5\x41\x57\x49\x89\xFF\x41\x56\x41\x55\x41\x54\x4D\x89\xC4)");

#endif
        auto m_hook = funchook_create();
        funchook_prepare(m_hook, (void **) &m_pHostSay, (void *) &DetourHostSay);
        funchook_install(m_hook, 0);
    } else {
        Message("Not detouring HostSay\n");
        Message("If you want chat events to work you need to have a Metamod plugin which fires the player_chat event\n");
    }
    // Call register convars
    g_pCVar = Framework::CVar();   // set magic global for metamod
    ConVar_Register(FCVAR_GAMEDLL | FCVAR_HIDDEN);

    return true;
}


bool WSTPlugin::Unload(char *error, size_t maxlen) {
    ConVar_Unregister();
    return true;
}

bool WSTPlugin::Pause(char *error, size_t maxlen) {
    return true;
}

bool WSTPlugin::Unpause(char *error, size_t maxlen) {
    return true;
}


void WSTPlugin::AllPluginsLoaded() {

}

const char *WSTPlugin::GetLicense() {
    return "GPL3";
}

const char *WSTPlugin::GetVersion() {
    return "1.2.0";
}

const char *WSTPlugin::GetDate() {
    return __DATE__;
}

const char *WSTPlugin::GetLogTag() {
    return "WST";
}

const char *WSTPlugin::GetAuthor() {
    return "will";
}

const char *WSTPlugin::GetDescription() {
    return "Will's CS2 Surftimer";
}

const char *WSTPlugin::GetName() {
    return "Will's CS2 Surftimer";
}

const char *WSTPlugin::GetURL() {
    return "https://github.com/ws-cs2/cs2-surftimer";
}
