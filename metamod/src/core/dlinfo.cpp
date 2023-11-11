#include "dlinfo.h"

#include <tier0/memdbgon.h>

#ifdef _WIN32
#include <psapi.h>
#else
#include <cstdint>
#include <elf.h>
#include <link.h>
#endif


#ifdef WIN32

int dlinfo(HINSTANCE handle, dlinfo_t* out)
{
    // https://learn.microsoft.com/en-us/windows/win32/api/psapi/ns-psapi-moduleinfo
    _MODULEINFO module_info{};
    if (!GetModuleInformation(GetCurrentProcess(), handle, &module_info, sizeof(_MODULEINFO)))
    {
        return 1;
    }

    out->address = module_info.lpBaseOfDll;
    out->size = module_info.SizeOfImage;
    return 0;
}

#else

    // https://github.com/Source2ZE/CS2Fixes/blob/db679357c84e61c979623b15b1454537c128cf56/src/utils/plat_unix.cpp
// https://github.com/alliedmodders/sourcemod/blob/master/core/logic/MemoryUtils.cpp#L502-L587

#define PAGE_SIZE           4096
#define PAGE_ALIGN_UP(x)    ((x + PAGE_SIZE - 1) & ~(PAGE_SIZE - 1))

int dlinfo(HINSTANCE hModule, dlinfo_t* out)
{
    struct link_map* dlmap = (struct link_map*)hModule;
    Dl_info info;
    Elf64_Ehdr* file;
    Elf64_Phdr* phdr;
    uint16_t phdrCount;

    if (!dladdr((void*)dlmap->l_addr, &info))
    {
        return 1;
    }

    if (!info.dli_fbase || !info.dli_fname)
    {
        return 2;
    }

    /* This is for our insane sanity checks :o */
    uintptr_t baseAddr = reinterpret_cast<uintptr_t>(info.dli_fbase);
    file = reinterpret_cast<Elf64_Ehdr*>(baseAddr);

    /* Check ELF magic */
    if (memcmp(ELFMAG, file->e_ident, SELFMAG) != 0)
    {
        return 3;
    }

    /* Check ELF version */
    if (file->e_ident[EI_VERSION] != EV_CURRENT)
    {
        return 4;
    }

    /* Check ELF endianness */
    if (file->e_ident[EI_DATA] != ELFDATA2LSB)
    {
        return 5;
    }

    /* Check ELF architecture */
    if (file->e_ident[EI_CLASS] != ELFCLASS64 || file->e_machine != EM_X86_64)
    {
        return 6;
    }

    /* For our purposes, this must be a dynamic library/shared object */
    if (file->e_type != ET_DYN)
    {
        return 7;
    }

    phdrCount = file->e_phnum;
    phdr = reinterpret_cast<Elf64_Phdr*>(baseAddr + file->e_phoff);

    for (uint16_t i = 0; i < phdrCount; i++)
    {
        Elf64_Phdr& hdr = phdr[i];

        /* We only really care about the segment with executable code */
        if (hdr.p_type == PT_LOAD && hdr.p_flags == (PF_X | PF_R))
        {
            /* From glibc, elf/dl-load.c:
             * c->mapend = ((ph->p_vaddr + ph->p_filesz + GLRO(dl_pagesize) - 1)
             *             & ~(GLRO(dl_pagesize) - 1));
             *
             * In glibc, the segment file size is aligned up to the nearest page size and
             * added to the virtual address of the segment. We just want the size here.
             */
            out->address = (void*)(baseAddr + hdr.p_paddr);
            out->size = PAGE_ALIGN_UP(hdr.p_filesz);
            break;
        }
    }

    return 0;
}
#endif