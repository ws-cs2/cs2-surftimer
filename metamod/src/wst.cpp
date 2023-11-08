#include "wst.h"

void Message(const char *msg, ...)
{
	va_list args;
	va_start(args, msg);

	char buf[1024] = {};
	V_vsnprintf(buf, sizeof(buf) - 1, msg, args);

	ConColorMsg(Color(255, 0, 255, 255), "[WST_MM] %s", buf);

	va_end(args);
}

// #define WST_DEBUG

void DEBUG_Message(const char *msg, ...)
{
#ifdef WST_DEBUG
	va_list args;
	va_start(args, msg);
	Message(msg, args);
	va_end(args);
#endif
}

WSTPlugin g_WSTPlugin;
PLUGIN_EXPOSE(WSTPlugin, g_WSTPlugin);

// Called by wst lua plugin to save records to disk
// This should only be called by the sever console
CON_COMMAND_F(wst_mm_save_record, "Save a record to disk", FCVAR_GAMEDLL | FCVAR_HIDDEN)
{
	// wst_mm_save_record <map_name> <steam_id> <time> <player name in quotes>
	// wst_mm_save_record surf_beginner "STEAM_0:1:123456789" 50 "player name"
	if (args.ArgC() != 5)
	{
		Message("Usage: wst_mm_save_record <map_name> <steam_id> <time> <player name in quotes>\n");
		// print args
		for (int i = 0; i < args.ArgC(); i++)
		{
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
	if (kv->LoadFromFile(g_pFullFileSystem, filePath, "MOD"))
	{
		// Find the data subkey
		data = kv->FindKey("data", true);
	}
	else
	{
		// Initialize the KeyValues structure if the file doesn't exist
		kv->SetString("version", "_1.0");
		data = kv->FindKey("data", true); // Create "data" if it doesn't exist
		DEBUG_Message("INITIALIZED NEW DATA STRUCTURE\n");
	}

	// Find or create the subkey for this record
	KeyValues *playerData = data->FindKey(steam_id, true);

	// Check if the new time is better than the existing time, or if it's a new record
	const char *existingTimeStr = playerData->GetString("time", nullptr);
	if (existingTimeStr == nullptr || time < atof(existingTimeStr))
	{
		// Update the record
		playerData->SetFloat("time", time);
		playerData->SetString("name", player_name);
		if (kv->SaveToFile(g_pFullFileSystem, filePath, "MOD"))
		{
			Message("Record saved successfully for %s on %s [%f]\n", player_name, map_name, time);
		}
		else
		{
			Message("Failed to save record for %s on %s [%f]\n", player_name, map_name, time);
		}
	}
	else
	{
		Message("Record not saved for %s on %s [%f] because it's not better than the existing record [%s]\n", player_name, map_name, time, existingTimeStr);
	}
}

bool WSTPlugin::Load(PluginId id, ISmmAPI *ismm, char *error, size_t maxlen, bool late)
{
	PLUGIN_SAVEVARS();

	Message("Plugin Loaded\n");

	// This cursed shit is required to register console commands, yay we have to set some rando ass global with some magical incarntation
	// see hl2sdk/tier1/convar.cpp
	GET_V_IFACE_CURRENT(GetEngineFactory, g_pCVar, ICvar, CVAR_INTERFACE_VERSION);
	GET_V_IFACE_ANY(GetFileSystemFactory, g_pFullFileSystem, IFileSystem, FILESYSTEM_INTERFACE_VERSION);

	// Call register convars
	ConVar_Register(FCVAR_GAMEDLL | FCVAR_HIDDEN);

	return true;
}

bool WSTPlugin::Unload(char *error, size_t maxlen)
{
	return true;
}

bool WSTPlugin::Pause(char *error, size_t maxlen)
{
	return true;
}

bool WSTPlugin::Unpause(char *error, size_t maxlen)
{
	return true;
}

void WSTPlugin::AllPluginsLoaded()
{
}

const char *WSTPlugin::GetLicense()
{
	return "GPL3";
}

const char *WSTPlugin::GetVersion()
{
	return "1.0.0";
}

const char *WSTPlugin::GetDate()
{
	return __DATE__;
}

const char *WSTPlugin::GetLogTag()
{
	return "WST";
}

const char *WSTPlugin::GetAuthor()
{
	return "will";
}

const char *WSTPlugin::GetDescription()
{
	return "Will's CS2 Surftimer";
}

const char *WSTPlugin::GetName()
{
	return "CS2SurfTimer";
}

const char *WSTPlugin::GetURL()
{
	return "https://github.com/ws-cs2/cs2-surftimer";
}
