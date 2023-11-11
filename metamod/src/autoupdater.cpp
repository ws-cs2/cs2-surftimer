#include "autoupdater.h"
#include "core/common.h"
#include "core/framework.h"
#include "core/whereami.h"
#include <filesystem>
#include <vector>

#include "lua_file.h"

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
}


void AutoUpdater::updateLuaScript() {
    ensureDirectoriesExist();
    sendHTTPRequest();
}

AutoUpdater::AutoUpdater() {
    this->gameDirPath = getGameDir();
}

AutoUpdater::~AutoUpdater() {}

void AutoUpdater::sendHTTPRequest() {
    Message("[Autoupdate] Sending HTTP Request\n");
    auto req = Framework::SteamHTTP()->CreateHTTPRequest(k_EHTTPMethodGET, "https://raw.githubusercontent.com/ws-cs2/cs2-surftimer/main/autoupdate.txt");

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

    // for each line create a LuaFile object
    for (auto& line : lines) {
        std::string url = "https://raw.githubusercontent.com/ws-cs2/cs2-surftimer/main/" + line;
        std::filesystem::path path = this->gameDirPath / line; // Correctly combine paths
        LuaFile* luaFile = new LuaFile(path, url.c_str());
        luaFile->updateFile();
    }

}
