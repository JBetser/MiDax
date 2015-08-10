using System;
using System.Windows;
using System.Windows.Threading;

namespace SampleWPFTrader.Common
{
    /// <Summary>
    ///
    /// IG Trader WPF Sample Application 
    /// 
    /// SmartDispatcher. Class that Abstracts threading issues and always ensures that we call events on the same thread as the one in which we raised the event on.
    ///
    /// Copyright 2014 IG Index
    ///
    /// Licensed under the Apache License, Version 2.0 (the 'License')
    /// You may not use this file except in compliance with the License.
    /// You may obtain a copy of the license at 
    /// http://www.apache.org/licenses/LICENSE-2.0
    ///
    /// Unless required by applicable law or agreed to in writing, software
    /// distributed under the License is distributed on an 'AS IS' BASIS,
    /// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
    /// See the License for the specific language governing permissions and
    /// limitations under the License.
    ///
    public class SmartDispatcher
    {
        private static bool _designer = false;
        private static Dispatcher _instance;

        public static void BeginInvoke(Action a)
        {
            BeginInvoke(a, false);
        }

        public static void BeginInvoke(Action a, bool forceInvoke)
        {
            if (_instance == null)
            {
                RequireInstance();
            }

            // If the current thread is the user interface thread, skip the
            // dispatcher and directly invoke the Action.
            if (_instance != null)
            {
                if (((forceInvoke && _instance != null) || !_instance.CheckAccess()) && !_designer)
                {
                    _instance.BeginInvoke(a);
                }
                else
                {
                    a();
                }
            }
            else
            {
                if (_designer || Application.Current == null)
                {
                    a();
                }
                }
        }

        private static void RequireInstance()
        {
            // Design-time is more of a no-op, won't be able to resolve the
            // dispatcher if it isn't already set in these situations.
            if (_designer || Application.Current == null)
            {
                return;
            }

            // Attempt to use the RootVisual of the plugin to retrieve a
            // dispatcher instance. This call will only succeed if the current
            // thread is the UI thread.
            try
            {
                _instance = Application.Current.Dispatcher;
            }
            catch (Exception e)
            {
                throw new InvalidOperationException("The first time SmartDispatcher is used must be from a user interface thread. Consider having the application call Initialize, with or without an instance.", e);
            }

            if (_instance == null)
            {
                throw new InvalidOperationException("Unable to find a suitable Dispatcher instance.");
            }
        }

        /// <summary>
        /// Initializes the SmartDispatcher system with the dispatcher
        /// instance and logger
        /// </summary>
        /// <param name="dispatcher">The dispatcher instance.</param>
        public static void Initialize(Dispatcher dispatcher)
        {
            if (dispatcher == null)
            {
                throw new ArgumentNullException("dispatcher");
            }

            _instance = dispatcher;
        }

    }
}

