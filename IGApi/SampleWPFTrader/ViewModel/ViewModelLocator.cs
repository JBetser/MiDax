using GalaSoft.MvvmLight.Ioc;
using Microsoft.Practices.ServiceLocation;

namespace SampleWPFTrader.ViewModel
{
    ///   
    /// <Summary>
    ///
    /// IG API - Sample Client - ViewModelLocator.cs
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
    /// This class contains static references to all the view models in the
    /// application and provides an entry point for the bindings.
    /// 
    ///  In App.xaml:
    ///  <Application.Resources>
    ///      <vm:ViewModelLocator xmlns:vm="clr-namespace:SampleWPFTrader"
    ///                           x:Key="Locator" />
    ///  
    /// In the View:
    ///  DataContext="{Binding Source={StaticResource Locator}, Path=ViewModelName}"
    ///
    ///  You can also use Blend to do all this with the tool's support.
    ///  See http://www.galasoft.ch/mvvm
    ///
    ///</summary>
    public class ViewModelLocator
    {
        /// <summary>
        /// Initializes a new instance of the ViewModelLocator class.
        /// </summary>
        public ViewModelLocator()
        {
            ServiceLocator.SetLocatorProvider(() => SimpleIoc.Default);

            ////if (ViewModelBase.IsInDesignModeStatic)
            ////{
            ////    // Create design time view services and models
            ////    SimpleIoc.Default.Register<IDataService, DesignDataService>();
            ////}
            ////else
            ////{
            ////    // Create run time view services and models
            ////    SimpleIoc.Default.Register<IDataService, DataService>();
            ////}

            SimpleIoc.Default.Register<MainViewModel>();
        }

        public MainViewModel Main
        {
            get
            {
                return ServiceLocator.Current.GetInstance<MainViewModel>();
            }
        }
        
        public static void Cleanup()
        {
            // TODO Clear the ViewModels
        }
    }
}