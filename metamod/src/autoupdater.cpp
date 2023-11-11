#include "autoupdater.h"
#include "core/common.h"
#include "core/framework.h"
#include "core/whereami.h"
#include <filesystem>
#include <vector>

#include "script_or_zone_file.h"

namespace fs = std::filesystem;

std::filesystem::path getPluginDir() {
    // Create a buffer with sufficient space
    std::string plugin_path;
    int plugin_path_size = wai_getModulePath(nullptr, 0, nullptr);
    plugin_path.resize(plugin_path_size);

    // Read the path
    int plugin_directory_path_size;
    wai_getModulePath(plugin_path.data(), plugin_path_size, &plugin_directory_path_size);

    // Shorten to the length of the parent directory
    plugin_path.resize(plugin_directory_path_size);

    std::filesystem::path filePath(plugin_path.data());

    return filePath;
}

std::filesystem::path getGameDir() {
    return getPluginDir().parent_path().parent_path().parent_path().parent_path();
}

bool startsWith(const std::string& fullString, const std::string& starting) {
    if (fullString.length() >= starting.length()) {
        return (0 == fullString.compare(0, starting.length(), starting));
    } else {
        return false;
    }
}

bool endsWith(const std::string& fullString, const std::string& ending) {
    if (fullString.length() >= ending.length()) {
        return (0 == fullString.compare(fullString.length() - ending.length(), ending.length(), ending));
    } else {
        return false;
    }
}



void createDirectoryIfNotExists(const fs::path& dirPath) {
    if (!fs::exists(dirPath)) {
        Message("Creating directory %s\n", dirPath.string().data());
        fs::create_directory(dirPath);
    } else {
        Message("Directory %s already exists\n", dirPath.string().data());
    }
}


void AutoUpdater::ensureDirectoriesExist() const {

    Message("Game dir: %s\n", this->gameDirPath.string().data());

    createDirectoryIfNotExists(this->gameDirPath / "scripts");
    createDirectoryIfNotExists(this->gameDirPath / "scripts" / "vscripts");
    createDirectoryIfNotExists(this->gameDirPath / "scripts" / "vscripts" / "wst");
    createDirectoryIfNotExists(this->gameDirPath / "scripts" / "wst_records");
    createDirectoryIfNotExists(this->gameDirPath / "scripts" / "wst_zones");
    createDirectoryIfNotExists(this->gameDirPath / "scripts" / "wst_config");
}

void AutoUpdater::cleanScripts() const {
    Message("Cleaning scripts\n");
    fs::remove(this->gameDirPath / "scripts" / "vscripts" / "wst.lua");
    fs::remove_all(this->gameDirPath / "scripts" / "vscripts" / "wst");
    
    // find all files starting with wst- and ending with .lua in the vsctipts directory (legacy)
    for (const auto& entry : fs::directory_iterator(this->gameDirPath / "scripts" / "vscripts")) {
        if (startsWith(entry.path().filename().string(), "wst-") &&
            endsWith(entry.path().filename().string(), ".lua")) {
            fs::remove(entry.path());
        }
    }
}


void AutoUpdater::updateLuaScript() {
    ensureDirectoriesExist();
    sendHTTPRequest("https://raw.githubusercontent.com/ws-cs2/cs2-surftimer/main/autoupdate_scripts.txt");
}

void AutoUpdater::updateZoneFiles() {
    ensureDirectoriesExist();
    sendHTTPRequest("https://raw.githubusercontent.com/ws-cs2/cs2-surftimer/main/autoupdate_zones.txt");
}

AutoUpdater::AutoUpdater() {
    this->gameDirPath = getGameDir();
}

AutoUpdater::~AutoUpdater() {}

void AutoUpdater::sendHTTPRequest(const char* url) {
    Message("[Autoupdate] Sending HTTP Request -> %s\n", url);
    auto req = Framework::SteamHTTP()->CreateHTTPRequest(k_EHTTPMethodGET, url);

    std::unique_ptr<HTTPCallback> http_callback = std::make_unique<HTTPCallback>();
    http_callback->SetGameserverFlag();

    Framework::SteamHTTP()->SetHTTPRequestContextValue(req, reinterpret_cast<uintptr_t>(http_callback.get()));


    SteamAPICall_t call;
    if (!Framework::SteamHTTP()->SendHTTPRequest(req, &call)) {
        Message("[Autoupdate] Failed to send HTTP Request\n");
        return;
    }

    http_callback->Set(call, this, &AutoUpdater::handleHTTPResponse);
    this->http_callbacks.emplace_back(std::move(http_callback));
}

void AutoUpdater::handleHTTPResponse(HTTPRequestCompleted_t *response, bool failed) {
    Message("[Autoupdate] Received Response\n");

    for (std::unique_ptr<HTTPCallback>& http_callback : this->http_callbacks)
    {
        if (reinterpret_cast<uintptr_t>(http_callback.get()) == response->m_ulContextValue)
        {
            std::swap(http_callback, this->http_callbacks.back());
            this->http_callbacks.pop_back();
        }
    }

    uint32_t responseSize;
    Framework::SteamHTTP()->GetHTTPResponseBodySize(response->m_hRequest, &responseSize);
    std::vector<uint8_t> responseData(responseSize);
    Framework::SteamHTTP()->GetHTTPResponseBodyData(response->m_hRequest, responseData.data(), responseSize);

    // read the response data into a string
    std::string responseString(reinterpret_cast<const char *>(responseData.data()), responseSize);
    // split the string by newlines
    std::vector<std::string> lines;
    std::string line;
    std::istringstream iss(responseString);
    while (std::getline(iss, line)) {
        Message("Found line: %s\n", line.c_str());
        lines.push_back(line);
    }

    // for each line create a ScriptOrZoneFile object
    for (auto& line : lines) {
        std::string url = "https://raw.githubusercontent.com/ws-cs2/cs2-surftimer/main/" + line;
        std::filesystem::path path = this->gameDirPath / line; // Correctly combine paths
        ScriptOrZoneFile* luaFile = new ScriptOrZoneFile(path, url.c_str());
        luaFile->updateFile();
    }

}
