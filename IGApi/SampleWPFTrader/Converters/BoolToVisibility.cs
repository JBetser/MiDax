using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace SampleWPFTrader.Converters
{
    /// <Summary>
    ///
    /// IG Trader WPF Sample Application 
    /// 
    /// BoolToVisibility Converter.
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
	public class BoolToVisibility : IValueConverter
	{
        public bool DownloadingData { get; set; }

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            bool inveter = false;
            if (parameter != null)
            {
                bool.TryParse((string)parameter, out inveter);
            }

            return value is bool
                       ? (DownloadingData == (bool)value ^ inveter ? Visibility.Visible : Visibility.Collapsed)
                       : Visibility.Visible;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }

	}
}
