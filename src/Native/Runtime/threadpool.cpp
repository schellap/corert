// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#include <windows.h>
#include <stdio.h>
#include <errno.h>
#include <evntprov.h>
#include "common.h"
#include "CommonTypes.h"
#include "CommonMacros.h"
#include "daccess.h"
#include "PalRedhawkCommon.h"
#include "PalRedhawk.h"
#include "assert.h"
#include "slist.h"
#include "gcrhinterface.h"
#include "varint.h"
#include "regdisplay.h"
#include "StackFrameIterator.h"
#include "thread.h"
#include "holder.h"
#include "Crst.h"
#include "event.h"
#include "RWLock.h"
#include "threadpool.h"
#include "RuntimeInstance.h"
#include "ObjectLayout.h"
#include "TargetPtrs.h"
#include "eetype.h"
#include "slist.inl"
#include "GCMemoryHelpers.h"
#include "threadstore.h"

extern "C" void RhpReversePInvoke2(ReversePInvokeFrame* pRevFrame);
extern "C" void RhpReversePInvokeReturn(ReversePInvokeFrame* pRevFrame);
extern "C" int32_t RhpEnableConservativeStackReporting();

namespace
{
struct ThreadWork
{
    ThreadWork(void* pCallback, void* pParameter)
        : m_pCallback(pCallback)
        , m_pParameter(pParameter)
    { }

    void* m_pCallback;
    void* m_pParameter;
};
}

ThreadPool* GetThreadPool()
{
    return GetRuntimeInstance()->GetThreadPool();
}

COOP_PINVOKE_HELPER(void, RhpPrintf, (char* chars, int id))
{
    printf("%d", id);
    printf(": ");
    printf(chars);
    printf("\n");
}

COOP_PINVOKE_HELPER(bool, RhpRequestWorkerThread, (void* pCallback))
{
    return GetThreadPool()->RequestWorkerThread(pCallback);
}

COOP_PINVOKE_HELPER(bool, RhpRequestDedicatedThread, (void* pCallback, void* pParameter))
{
    return GetThreadPool()->RequestDedicatedThread(pCallback, pParameter);
}

ThreadPool* ThreadPool::Create(RuntimeInstance* pRuntimeInstance)
{
    NewHolder<ThreadPool> pNewThreadPool = new (nothrow) ThreadPool();
    if (NULL == pNewThreadPool)
        return NULL;

    pNewThreadPool->m_pRuntimeInstance = pRuntimeInstance;

    pNewThreadPool.SuppressRelease();
    return pNewThreadPool;
}

void ThreadPool::Destroy()
{
    delete this;
}

ThreadPool::ThreadPool()
{
}

ThreadPool::~ThreadPool()
{
}

Int32 ThreadPool::RequestWorkerThread(void* pCallback)
{
    typedef void (* ActionPtr) ();
    ActionPtr pFnCallback = (ActionPtr) pCallback;
    pFnCallback();
    return 0;
}


unsigned long __stdcall ThreadPool::DedicatedThreadProc(void* pParameter)
{
    ReversePInvokeFrame frame;
    RhpReversePInvoke2(&frame);
    RhpEnableConservativeStackReporting();

    typedef void (* ActionTPtr) (void*);

    ThreadWork* work = (ThreadWork*) pParameter;
    ActionTPtr pFnCallback = (ActionTPtr) work->m_pCallback;

    pFnCallback(work->m_pParameter);

    RhpReversePInvokeReturn(&frame);

    return 0;
}

Int32 ThreadPool::RequestDedicatedThread(void* pCallback, void* pParameter)
{
    ThreadWork* pWork = new (nothrow) ThreadWork(pCallback, pParameter);
    DWORD dwThreadId;

    HANDLE hThread = ::CreateThread(
        nullptr,
        4096,
        DedicatedThreadProc,
        pWork,
        0,
        &dwThreadId);

    if (hThread == nullptr)
    {
        return GetLastError();
    }

    return 0;
}

void ThreadPool::SetupThread()
{
}

void ThreadPool::DestroyThread()
{
}

