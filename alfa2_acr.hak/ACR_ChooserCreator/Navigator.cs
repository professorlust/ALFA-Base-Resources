﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;

namespace ACR_ChooserCreator
{
    public static class Navigators
    {
        public volatile static Navigator CreatureNavigator = new Navigator();
        public volatile static Navigator ItemNavigator = new Navigator();
        public volatile static Navigator LightNavigator = new Navigator();
        public volatile static Navigator PlaceableNavigator = new Navigator();
        public volatile static Navigator WaypointNavigator = new Navigator();
        public volatile static Navigator VisualEffectNavigator = new Navigator();
        public volatile static Navigator TrapNavigator = new Navigator();
    }

    public class Navigator: ALFA.Shared.IBackgroundLoadedResource, IDisposable
    {
        private volatile bool _resourcesLoaded = false;

        /// <summary>
        /// Wait for resources to become available.
        /// </summary>
        /// <param name="Wait">Supplies true to perform a blocking wait, else
        /// false to return immediately with the current availability status.
        /// </param>
        /// <returns>True if the resources are available to be accessed, else
        /// false if they are not yet loaded.</returns>
        public bool WaitForResourcesLoaded(bool Wait)
        {
            if (_resourcesLoaded) return true;
            return ResourcesLoadedEvent.WaitOne(Wait ? Timeout.Infinite : 0);
        }

        /// <summary>
        /// Dispose the object.
        /// </summary>
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// Invoked to perform cleanup of the object.
        /// </summary>
        /// <param name="Disposing">Supplies true if cleanup is occuring for
        /// IDisposable.Dispose(), else false if cleanup is occuring for
        /// finalization (which must have skippsed IDisposable.Dispose() as the
        /// normal Dispose() path suppresses finalization).</param>
        private void Dispose(bool Disposing)
        {
            if (!Disposed)
            {
                if (Disposing)
                {
                    //
                    // Clean up managed resources.
                    //

                    ResourcesLoadedEvent.Dispose();
                }

                //
                // Clean up unmanaged resources.
                //

                Disposed = true;
            }
        }

        /// <summary>
        /// Finalizer; only invoked if IDisposable.Dispose() was not already
        /// invoked.  Joins common code at Dispose(bool) to clean up the
        /// object.
        /// </summary>
        ~Navigator()
        {
            Dispose(false);
        }

        public NavigatorCategory bottomCategory = new NavigatorCategory();

        /// <summary>
        /// Mark resources as fully loaded after initialization completes.
        /// </summary>
        internal void SetResourcesLoaded()
        {
            ResourcesLoadedEvent.Set();
            _resourcesLoaded = true;
        }

        /// <summary>
        /// The wait event used to signal resource load completion.
        /// </summary>
        private ManualResetEvent ResourcesLoadedEvent = new ManualResetEvent(false);
        /// <summary>
        /// True if the disposer has run.
        /// </summary>
        private bool Disposed = false;
    }
}
