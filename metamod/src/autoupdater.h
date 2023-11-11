#ifndef WST_AUTOUPDATER_H
#define WST_AUTOUPDATER_H

#include <filesystem>
#include <vector>
#include "steam/isteamhttp.h"

namespace fs = std::filesystem;

class AutoUpdater {
public:
    AutoUpdater();
    ~AutoUpdater();

    void updateLuaScript();
    void updateZoneFiles();

    void cleanScripts() const;

    void ensureDirectoriesExist() const;

    std::filesystem::path gameDirPath;

private:
    // Networking related functions (if necessary)
    void sendHTTPRequest(const char* url);
    void handleHTTPResponse(HTTPRequestCompleted_t* response, bool failed);

    // Track HTTP request callbacks.
    using HTTPCallback = CCallResult<AutoUpdater, HTTPRequestCompleted_t>;
    std::vector<std::unique_ptr<HTTPCallback>> http_callbacks;

    // Add any other private member variables or functions here


};


#endif //WST_AUTOUPDATER_H
