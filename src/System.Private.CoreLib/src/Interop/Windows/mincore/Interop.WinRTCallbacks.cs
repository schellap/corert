// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Internal.Runtime.Augments;
using System;
using System.Runtime.InteropServices;
using System.Collections.Generic;
using System.Runtime;
using System.Runtime.CompilerServices;

internal static unsafe partial class Interop
{
    internal static WinRTInteropCallbacks WinRTInteropCallback = new mincore.WinRTInteropImpl();

    internal static unsafe partial class mincore
    {
        internal class WinRTInteropImpl : WinRTInteropCallbacks
        {
            public override byte[] ComputeSHA1(byte[] plainText)
            {
                throw new NotImplementedException();
            }

            public override IList<T> CreateSystemCollectionsGenericList<T>()
            {
                throw new NotImplementedException();
            }

            public override object CreateTimer(Delegate timerElapsedHandler, TimeSpan delay)
            {
                throw new NotImplementedException();
            }

            public override Delegate CreateTimerDelegate(Action callback)
            {
                throw new NotImplementedException();
            }

            public override object GetCOMWeakReferenceTarget(object weakReference)
            {
                throw new NotImplementedException();
            }

            public override object GetCurrentCoreDispatcher()
            {
                throw new NotImplementedException();
            }

            public override int GetHijriDateAdjustment()
            {
                throw new NotImplementedException();
            }

            public override int GetJapaneseEraCount()
            {
                throw new NotImplementedException();
            }

            public override bool GetJapaneseEraInfo(int era, out DateTimeOffset startDate, out string eraName, out string abbreviatedEraName)
            {
                throw new NotImplementedException();
            }

            public override string GetLanguageDisplayName(string cultureName)
            {
                throw new NotImplementedException();
            }

            public override string GetRegionDisplayName(string isoCountryCode)
            {
                throw new NotImplementedException();
            }

            public override object GetResourceMap(string subtreeName)
            {
                throw new NotImplementedException();
            }

            public override string GetResourceString(object resourceMap, string resourceName, string languageName)
            {
                throw new NotImplementedException();
            }

            public override object GetUserDefaultCulture()
            {
                throw new NotImplementedException();
            }

            public override void InitTracingStatusChanged(Action<bool> tracingStatusChanged)
            {
                tracingStatusChanged(false);
            }

            public override bool IsAppxModel()
            {
                throw new NotImplementedException();
            }

            public override void PostToCoreDispatcher(object dispatcher, Action<object> action, object state)
            {
                throw new NotImplementedException();
            }

            public override object ReadFileIntoStream(string name)
            {
                throw new NotImplementedException();
            }

            public override void ReleaseTimer(object timer, bool cancelled)
            {
                throw new NotImplementedException();
            }

            public override bool ReportUnhandledError(Exception ex)
            {
                throw new NotImplementedException();
            }

            public override void SetCOMWeakReferenceTarget(object weakReference, object target)
            {
                throw new NotImplementedException();
            }

            public override void SetGlobalDefaultCulture(object culture)
            {
                throw new NotImplementedException();
            }

            static volatile Action m_threadPoolDispatchCallback;
            public override void SetThreadpoolDispatchCallback(Action callback)
            {
                m_threadPoolDispatchCallback = callback;
            }

            internal static void WorkerThreadRequestCallback()
            {
                m_threadPoolDispatchCallback();
            }

            internal static void DedicatedThreadRequestCallback(object state)
            {
                LongRunningThreadpoolWork work = (LongRunningThreadpoolWork)state;
                work.Work();
                work.Dispose();
            }

            public override void SubmitThreadpoolDispatchCallback()
            {
                Action action = WorkerThreadRequestCallback;
                // RuntimeImports.RequestWorkerThread(action.GetNativeFunctionPointer());
                SubmitLongRunningThreadpoolWork(action);
            }

            internal class LongRunningThreadpoolWork : IDisposable
            {
                public Action Work;
                private IntPtr handle;

                public LongRunningThreadpoolWork(Action callback)
                {
                    handle = RuntimeImports.RhHandleAlloc(this, GCHandleType.Normal);
                    Work = callback;
                }

                public void Dispose()
                {
                    RuntimeImports.RhHandleFree(handle);
                }
            }

            public override void SubmitLongRunningThreadpoolWork(Action callback)
            {
                Action<object> action = DedicatedThreadRequestCallback;
                RuntimeImports.RequestDedicatedThread(action.GetNativeFunctionPointer(), new LongRunningThreadpoolWork(callback));
            }

            public override void TraceOperationCompletion(CausalityTraceLevel traceLevel, CausalitySource source, Guid platformId, ulong operationId, AsyncStatus status)
            {
                throw new NotImplementedException();
            }

            public override void TraceOperationCreation(CausalityTraceLevel traceLevel, CausalitySource source, Guid platformId, ulong operationId, string operationName, ulong relatedContext)
            {
                throw new NotImplementedException();
            }

            public override void TraceOperationRelation(CausalityTraceLevel traceLevel, CausalitySource source, Guid platformId, ulong operationId, CausalityRelation relation)
            {
                throw new NotImplementedException();
            }

            public override void TraceSynchronousWorkCompletion(CausalityTraceLevel traceLevel, CausalitySource source, CausalitySynchronousWork work)
            {
                throw new NotImplementedException();
            }

            public override void TraceSynchronousWorkStart(CausalityTraceLevel traceLevel, CausalitySource source, Guid platformId, ulong operationId, CausalitySynchronousWork work)
            {
                throw new NotImplementedException();
            }
        }
    }
}