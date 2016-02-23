// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using global::System;
using global::System.Reflection;
using global::System.Collections.Generic;
using global::System.Diagnostics;

using global::Internal.Runtime.Augments;

using global::Internal.Reflection.Core;
using global::Internal.Reflection.Core.Execution;

using global::Internal.Metadata.NativeFormat;

using ReflectionMapBlob = Internal.Runtime.ReflectionMapBlob;

namespace Internal.Reflection.Execution
{
    internal sealed partial class ExecutionEnvironmentImplementation : ExecutionEnvironment
    {
        /// <summary>
        /// List of callbacks to execute when a module gets registered.
        /// </summary>
        List<Action<IntPtr>> _moduleRegistrationCallbacks = new List<Action<IntPtr>>();

        public unsafe ExecutionEnvironmentImplementation()
        {
            _moduleToMetadataReader = new LowLevelDictionaryWithIEnumerable<IntPtr, MetadataReader>();
            
            // Metadata reader must be creater first as other callbacks might need it
            AddModuleRegistrationCallback(CreateMetadataReader);
            AddModuleRegistrationCallback(ModuleList.RegisterModule);
        }

        /// <summary>
        /// Add a new module registration callback. Invoke the callback for all currently
        /// registered modules.
        /// </summary>
        /// <param name="moduleRegistrationCallback">Callback gets passed the module handle</param>
        internal void AddModuleRegistrationCallback(Action<IntPtr> moduleRegistrationCallback)
        {
            _moduleRegistrationCallbacks.Add(moduleRegistrationCallback);
            int loadedModulesCount = RuntimeAugments.GetLoadedModules(null);
            IntPtr[] loadedModuleHandles = new IntPtr[loadedModulesCount];
            int loadedModules = RuntimeAugments.GetLoadedModules(loadedModuleHandles);
            Debug.Assert(loadedModulesCount == loadedModules);
            foreach (IntPtr moduleHandle in loadedModuleHandles)
            {
                moduleRegistrationCallback(moduleHandle);
            }
        }

        /// <summary>
        /// Register a new module. Call all module registration callbacks. The constructor immediately
        /// registers some callbacks so in this call the _moduleRegistrationCallbacks should never be null.
        /// </summary>
        /// <param name="moduleHandle">Module handle to register</param>
        public void RegisterModule(IntPtr moduleHandle)
        {
            lock (this)
            {
                foreach (var callback in _moduleRegistrationCallbacks)
                    callback(moduleHandle);
            }
        }

        /// <summary>
        /// Locate reflection blob in a given module and construct its metadata reader.
        /// </summary>
        /// <param name="moduleHandle">Module handle to register</param>
        unsafe void CreateMetadataReader(IntPtr moduleHandle)
        {
            uint* pBlob;
            uint cbBlob;

            if (RuntimeAugments.FindBlob(moduleHandle, (int)ReflectionMapBlob.EmbeddedMetadata, (IntPtr)(&pBlob), (IntPtr)(&cbBlob)))
            {
                MetadataReader reader = new MetadataReader((IntPtr)pBlob, (int)cbBlob);
                _moduleToMetadataReader.Add(moduleHandle, reader);
            }
        }
    }
}

