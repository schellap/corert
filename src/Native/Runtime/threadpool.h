// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

class Thread;
class RuntimeInstance;
typedef DPTR(RuntimeInstance) PTR_RuntimeInstance;

class ThreadPool
{
public:
    static ThreadPool* Create(RuntimeInstance* pRuntimeInstance);
    void Destroy();

    Int32 RequestWorkerThread(void* pCallback);
    Int32 RequestDedicatedThread(void* pCallback, void* pParameter);

    ~ThreadPool();
    
    static unsigned long __stdcall DedicatedThreadProc(void* pParameter);

private:
    ThreadPool();

    static void SetupThread();
    static void DestroyThread();

    RuntimeInstance* m_pRuntimeInstance;
};
