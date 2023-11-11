#include <vector>
#include <fstream>
#include "lua_file.h"
#include "core/framework.h"
#include "core/common.h"

LuaFile::LuaFile(std::filesystem::path path, const char *url) {
    this->path = path;
    this->url = url;
}

void LuaFile::updateFile() {
    Message("[LuaFile] Sending HTTP Request -> %s\n", this->url);
    auto req = Framework::SteamHTTP()->CreateHTTPRequest(k_EHTTPMethodGET, this->url);

    std::unique_ptr<HTTPCallback> http_callback = std::make_unique<HTTPCallback>();
    http_callback->SetGameserverFlag();

    Framework::SteamHTTP()->SetHTTPRequestContextValue(req, reinterpret_cast<uintptr_t>(http_callback.get()));


    SteamAPICall_t call;
    if (!Framework::SteamHTTP()->SendHTTPRequest(req, &call)) {
        Message("[LuaFile] Failed to send HTTP Request -> %s\n", this->url);
        return;
    }

    http_callback->Set(call, this, &LuaFile::handleHTTPResponse);
    this->http_callbacks.emplace_back(std::move(http_callback));
}

void LuaFile::handleHTTPResponse(HTTPRequestCompleted_t *response, bool failed) {
    Message("[LuaFile] Received Response -> %s\n", this->url);

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

    Message("[LuaFile] Writing to file -> %s\n", this->path.string().data());

    std::ofstream file;
    file.open(this->path);
    file.write(reinterpret_cast<const char *>(responseData.data()), responseSize);
    file.close();
}
