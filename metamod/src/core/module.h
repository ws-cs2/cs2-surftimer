#ifndef WST_MODULE_H
#define WST_MODULE_H

#pragma once

#include "dbg.h"
#include "interface.h"
#include "strtools.h"
#include "metamod_oslink.h"

#ifdef _WIN32
#include <Psapi.h>
#endif


#ifdef _WIN32
#define MODULE_PREFIX ""
#define MODULE_EXT ".dll"
#else
#define MODULE_PREFIX "lib"
#define MODULE_EXT ".so"
#endif

#ifdef _WIN32
#define ROOTBIN "/bin/win64/"
#define GAMEBIN "/csgo/bin/win64/"
#else
#define ROOTBIN "/bin/linuxsteamrt64/"
#define GAMEBIN "/csgo/bin/linuxsteamrt64/"
#endif

class CModule
{
public:
    CModule(const char *path, const char *module) :
            m_pszModule(module), m_pszPath(path)
    {
        char szModule[MAX_PATH];

        V_snprintf(szModule, MAX_PATH, "%s%s%s%s%s", Plat_GetGameDirectory(), path, MODULE_PREFIX, m_pszModule, MODULE_EXT);

        m_hModule = dlmount(szModule);

        if (!m_hModule)
            Error("Could not find %s\n", szModule);

#ifdef _WIN32
        MODULEINFO m_hModuleInfo;
        GetModuleInformation(GetCurrentProcess(), m_hModule, &m_hModuleInfo, sizeof(m_hModuleInfo));

        m_base = (void *)m_hModuleInfo.lpBaseOfDll;
        m_size = m_hModuleInfo.SizeOfImage;
#else
        if (int e = GetModuleInformation(szModule, &m_base, &m_size))
			Error("Failed to get module info for %s, error %d\n", szModule, e);
#endif
    }

    void *FindSignature(const byte *pData, size_t length)
    {
        unsigned char *pMemory;
        void *return_addr = nullptr;

        pMemory = (byte *)m_base;

        for (size_t i = 0; i < m_size; i++)
        {
            size_t Matches = 0;
            while (*(pMemory + i + Matches) == pData[Matches] || pData[Matches] == '\x2A')
            {
                Matches++;
                if (Matches == length)
                    return_addr = (void *)(pMemory + i);
            }
        }

        return return_addr;
    }

    // Will break with string containing \0 !!!
    void *FindSignature(const byte *pData)
    {
        size_t iSigLength = V_strlen((const char *)pData);

        return FindSignature(pData, iSigLength);
    }

    static byte *ConvertToByteArray(const char *str, size_t *outLength) {
        size_t len = strlen(str) / 4;  // Every byte is represented as \xHH
        byte *result = (byte *)malloc(len);

        for (size_t i = 0, j = 0; i < len; ++i, j += 4) {
            sscanf(str + j, "\\x%2hhx", &result[i]);
        }

        *outLength = len;
        return result;
    }

    void *FindSignature(const char *pData)
    {
        size_t iSigLength;
        byte *pByteArray = ConvertToByteArray(pData, &iSigLength);

        void *result = FindSignature(pByteArray, iSigLength);

        free(pByteArray);

        return result;
    }

    void *FindInterface(const char *name) {
        CreateInterfaceFn fn = (CreateInterfaceFn)dlsym(m_hModule, "CreateInterface");

        if (!fn) Error("Could not find CreateInterface in %s\n", m_pszModule);

        void *pInterface = fn(name, nullptr);

        if (!pInterface) Error("Could not find %s in %s\n", name, m_pszModule);

        return pInterface;
    }


    const char *m_pszModule;
    const char* m_pszPath;
    HINSTANCE m_hModule;
    void* m_base;
    size_t m_size;
};
#endif //WST_MODULE_H
