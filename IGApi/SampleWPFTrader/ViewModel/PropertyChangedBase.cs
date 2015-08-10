using System;
using System.Collections.Generic;
using System.ComponentModel;
using SampleWPFTrader.Common;

namespace SampleWPFTrader.ViewModel
{
    /// <Summary>
    ///
    /// IG API - Sample Client - PropertyChangedBase.cs
    ///
    /// Copyright 2014 IG Index 10/17/2014 14:55:06
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
    /// </Summary>
    public class PropertyChangedBase : INotifyPropertyChanged
    {
        /// <summary>
        /// A static set of argument instances, one per property name.
        /// </summary>
        private static Dictionary<string, PropertyChangedEventArgs> _argumentInstances = new Dictionary<string, PropertyChangedEventArgs>();

        /// <summary>
        /// The property changed event.
        /// </summary>
        public event PropertyChangedEventHandler PropertyChanged;

        /// <summary>
        /// Notify any listeners that the property value has changed.
        /// </summary>
        /// <param name="propertyName">The property name.</param>
        public void RaisePropertyChanged(string propertyName)
        {
            if (string.IsNullOrEmpty(propertyName))
            {
                throw new ArgumentException("PropertyName cannot be empty or null.");
            }

            PropertyChangedEventHandler handler = PropertyChanged;
            if (handler != null)
            {
                PropertyChangedEventArgs args;
                if (!_argumentInstances.TryGetValue(propertyName, out args))
                {
                    args = new PropertyChangedEventArgs(propertyName);
                    _argumentInstances[propertyName] = args;
                }

                // Fire the change event. The smart dispatcher will directly
                // invoke the handler if this change happened on the UI thread,
                // otherwise it is sent to the proper dispatcher.
                SmartDispatcher.BeginInvoke(delegate
                {
                    handler(this, args);
                });
            }
        }
    }
}
