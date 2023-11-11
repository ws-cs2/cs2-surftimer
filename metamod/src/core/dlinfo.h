//
// Created by RTX 3090 Gamer on 12/11/2023.
//

#ifndef WST_DLINFO_H
#define WST_DLINFO_H

#pragma once

#include <metamod_oslink.h>

struct dlinfo_t
{
    void* address{nullptr};
    size_t size{0};
};

int dlinfo(HINSTANCE module, dlinfo_t* info);


#endif //WST_DLINFO_H
