//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.ObjectModel;

namespace Microsoft.Azure.Functions.PowerShellWorker
{
    /// <summary>
    /// Function metadata for the PowerShellWorker module to query.
    /// </summary>
    public static class FunctionMetadata
    {
        private static ConcurrentDictionary<Guid, ReadOnlyDictionary<string, ReadOnlyBindingInfo>> OutputBindingCache
            = new ConcurrentDictionary<Guid, ReadOnlyDictionary<string, ReadOnlyBindingInfo>>();

        private static ConcurrentDictionary<Guid, Hashtable> OutputBindingValues = new ConcurrentDictionary<Guid, Hashtable>();

        /// <summary>
        /// Get the binding metadata for the given Runspace instance id.
        /// </summary>
        public static ReadOnlyDictionary<string, ReadOnlyBindingInfo> GetOutputBindingInfo(Guid runspaceInstanceId)
        {
            OutputBindingCache.TryGetValue(runspaceInstanceId, out var outputBindings);
            return outputBindings;
        }

        /// <summary>
        /// Get the Hashtable that is holding the output binding values for the given Runspace.
        /// </summary>
        internal static Hashtable GetOutputBindingValues(Guid runspaceInstanceId)
        {
            return OutputBindingValues.GetOrAdd(
                runspaceInstanceId,
                key => new Hashtable(StringComparer.OrdinalIgnoreCase));
        }

        /// <summary>
        /// Helper method to set the output binding metadata for the function that is about to run.
        /// </summary>
        internal static void RegisterFunctionMetadata(Guid instanceId, AzFunctionInfo functionInfo)
        {
            var outputBindings = functionInfo.OutputBindings;
            OutputBindingCache.AddOrUpdate(instanceId, outputBindings, (key, value) => outputBindings);
        }

        /// <summary>
        /// Helper method to clear the output binding metadata for the function that has done running.
        /// </summary>
        internal static void UnregisterFunctionMetadata(Guid instanceId)
        {
            OutputBindingCache.TryRemove(instanceId, out _);
        }
    }
}
