#ifndef WST_LUA_FILE_H
#define WST_LUA_FILE_H


#include <filesystem>
#include "steam/isteamhttp.h"

class LuaFile {
public:
    LuaFile(std::filesystem::path path, const char* url);
    ~LuaFile();

    void updateFile();

    std::filesystem::path path;
    const char* url;
private:
    void handleHTTPResponse(HTTPRequestCompleted_t* response, bool failed);
    // Track HTTP request callbacks.
    using HTTPCallback = CCallResult<LuaFile, HTTPRequestCompleted_t>;
    std::vector<std::unique_ptr<HTTPCallback>> http_callbacks;

};


#endif //WST_LUA_FILE_H
