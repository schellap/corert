// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
#pragma once
#include "ModuleHeaders.h"

class DispatchMap;

class ModuleManager
{
    void *                       m_pHeaderStart;
    void *                       m_pHeaderEnd;

    DispatchMap**                m_pDispatchMapTable;
    volatile void* volatile*     m_pGCStaticBase;
    static DECLSPEC_THREAD void* m_pThreadStaticBase;

    ModuleManager(void * pHeaderStart, void * pHeaderEnd)
        : m_pHeaderStart(pHeaderStart)
        , m_pHeaderEnd(pHeaderEnd)
        , m_pDispatchMapTable(nullptr)
        , m_pGCStaticBase(nullptr)
    { }

public:
    static ModuleManager * Create(void * pHeaderStart, void * pHeaderEnd);
    void * GetModuleSection(ModuleHeaderSection sectionId, int * length);
    DispatchMap ** GetDispatchMapLookupTable();

    void SetThreadStaticBase(void* pBase);
    bool SetGCStaticBaseInterlocked(void* pBase);
    void * GetThreadStaticBase();
    volatile void* volatile* GetGCStaticBase();

private:
    
    struct ModuleInfoRow
    {
        int32_t SectionId;
        int32_t Flags;
        void * Start;
        void * End;

        bool HasEndPointer();
        int GetLength();
    };
};
