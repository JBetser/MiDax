using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Input;

namespace SampleWPFTrader
{
    /// <Summary>
    ///
    /// IG Trader WPF Sample Application 
    ///    
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
    public class RelayCommand : ICommand
    {
        private readonly Action _handler;
        private bool _isEnabled;

        public RelayCommand(Action handler)
        {
            _handler = handler;
        }

        public bool IsEnabled
        {
            get { return _isEnabled; }
            set
            {
                if (value != _isEnabled)
                {
                    _isEnabled = value;
                    if (CanExecuteChanged != null)
                    {
                        CanExecuteChanged(this, EventArgs.Empty);
                    }
                }
            }
        }

        public bool CanExecute(object parameter)
        {
            return IsEnabled;
        }

        public event EventHandler CanExecuteChanged;

        public void Execute(object parameter)
        {
            _handler();
        }
    }

}
