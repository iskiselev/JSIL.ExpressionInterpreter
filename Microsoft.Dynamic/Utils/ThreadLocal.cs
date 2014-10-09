using System;


namespace Microsoft.Scripting.Utils
{
    /// <summary>
    /// Provides fast strongly typed thread local storage.  This is significantly faster than
    /// Thread.GetData/SetData.
    /// </summary>
    public class ThreadLocal<T>
    {
        private readonly StorageInfo _stores;

        public ThreadLocal()
        {
            _stores = new StorageInfo();
        }


        #region Public API

        /// <summary>
        /// Gets the StorageInfo for the current thread.
        /// </summary>
        public StorageInfo GetStorageInfo()
        {
            return _stores;
        }


        /// <summary>
        /// Helper class for storing the value.  We need to track if a ManagedThreadId
        /// has been re-used so we also store the thread which owns the value.
        /// </summary>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1034:NestedTypesShouldNotBeVisible")] // TODO
        public sealed class StorageInfo
        {
            public object Value;                                // the current value for the owning thread

            internal StorageInfo()
            {
            }
        }

        #endregion
    }
}