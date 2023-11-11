#ifndef WST_SCRIPT_OR_ZONE_FILE_H
#define WST_SCRIPT_OR_ZONE_FILE_H


#include <filesystem>
#include "steam/isteamhttp.h"

class ScriptOrZoneFile {
public:
    ScriptOrZoneFile(std::filesystem::path path, const char* url);
    ~ScriptOrZoneFile();

    void updateFile();

    std::filesystem::path path;
    const char* url;
private:
    void handleHTTPResponse(HTTPRequestCompleted_t* response, bool failed);
    // Track HTTP request callbacks.
    using HTTPCallback = CCallResult<ScriptOrZoneFile, HTTPRequestCompleted_t>;
    std::vector<std::unique_ptr<HTTPCallback>> http_callbacks;

};


#endif //WST_SCRIPT_OR_ZONE_FILE_H
