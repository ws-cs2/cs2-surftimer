#ifndef WST_VIRTUAL_H
#define WST_VIRTUAL_H

#include "platform.h"

#define CALL_VIRTUAL(retType, idx, ...) \
	CallVirtual<retType>(idx, __VA_ARGS__)


template <typename T, int index, typename ...Args>
constexpr T CallVFunc(void* pThis, Args... args) noexcept
{
    return reinterpret_cast<T(*)(void*, Args...)> (reinterpret_cast<void***>(pThis)[0][index])(pThis, args...);
}

template <typename T = void *>
inline T GetVMethod(uint32 uIndex, void *pClass)
{
    if (!pClass)
    {
        return T();
    }

    void **pVTable = *static_cast<void ***>(pClass);
    if (!pVTable)
    {
        return T();
    }

    return reinterpret_cast<T>(pVTable[uIndex]);
}

template <typename T, typename... Args>
inline T CallVirtual(uint32 uIndex, void *pClass, Args... args)
{
#ifdef _WIN32
    auto pFunc = GetVMethod<T(__thiscall *)(void *, Args...)>(uIndex, pClass);
#else
    auto pFunc = GetVMethod<T(__cdecl*)(void*, Args...)>(uIndex, pClass);
#endif
    if (!pFunc)
    {
        return T();
    }

    return pFunc(pClass, args...);
}


#endif //WST_VIRTUAL_H
