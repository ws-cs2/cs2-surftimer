#include "schemasystem.h"
#include "virtual.h"
#include <cstring>

void CSchemaSystemTypeScope::FindDeclaredClass(SchemaClassInfoData_t*& pClassInfo, const char* pszClassName)
{
#if WIN32
    CallVFunc<void, 2, SchemaClassInfoData_t*&, const char*>(this, pClassInfo, pszClassName);
#else
    pClassInfo = CallVFunc<SchemaClassInfoData_t*, 2, const char*>(this, pszClassName);
#endif
}

CSchemaSystemTypeScope* CSchemaSystem::FindTypeScopeForModule(const char* szpModuleName)
{
    return CallVFunc<CSchemaSystemTypeScope*, 13, const char*, void*>(this, szpModuleName, nullptr);
}

CSchemaSystemTypeScope* CSchemaSystem::GetServerTypeScope()
{
#if WIN32
    static CSchemaSystemTypeScope* pServerTypeScope = FindTypeScopeForModule("server.dll");
#else
    static CSchemaSystemTypeScope* pServerTypeScope = FindTypeScopeForModule("libserver.so"));
#endif

    return pServerTypeScope;
}

int32_t CSchemaSystem::GetServerOffset(const char* pszClassName, const char* pszPropName)
{
    SchemaClassInfoData_t* pClassInfo = nullptr;
    GetServerTypeScope()->FindDeclaredClass(pClassInfo, pszClassName);
    if (pClassInfo)
    {
        for (int i = 0; i < pClassInfo->m_iFieldsCount; i++)
        {
            auto& pFieldData = pClassInfo->m_pFieldsData[i];

            if (std::strcmp(pFieldData.m_pszName, pszPropName) == 0)
            {
                return pFieldData.m_iOffset;
            }
        }
    }

    return -1;
}