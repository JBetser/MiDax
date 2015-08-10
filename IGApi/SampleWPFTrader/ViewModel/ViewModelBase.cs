using IGPublicPcl;

namespace SampleWPFTrader.ViewModel
{
	/// <Summary>
	///
	/// IG Trader WPF Sample Application 
	/// 
	/// ViewModelBase
	///
	/// Copyright 2014 IG Index - ViewModelBase.cs
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
	public abstract class ViewModelBase : PropertyChangedBase
	{
		public static IgRestApiClient igRestApiClient;
		public static IGStreamingApiClient igStreamApiClient;

		public enum TradeSubscriptionType
		{
			Opu = 0,
			Wou = 1,
			Confirm = 2
		}
		
        public static string CurrentAccountId;
      
        public void InitialiseViewModel()
        {          
            igRestApiClient = new IgRestApiClient();           
            igStreamApiClient = new IGStreamingApiClient();            
        }

        public static bool LoggedIn { get; set; }             
  
        public string ConvertMarketStatusToString(dto.endpoint.type.MarketStatus marketStatus)
        {
            string strMarketStatus;

            switch (marketStatus)
            {
                case dto.endpoint.type.MarketStatus.CLOSED:
                    strMarketStatus = "Closed";
                    break;
                case dto.endpoint.type.MarketStatus.EDITS_ONLY:
                    strMarketStatus = "Edits Only";
                    break;
                case dto.endpoint.type.MarketStatus.OFFLINE:
                    strMarketStatus = "Offline";
                    break;
                case dto.endpoint.type.MarketStatus.ON_AUCTION:
                    strMarketStatus = "On Auction";
                    break;
                case dto.endpoint.type.MarketStatus.ON_AUCTION_NO_EDITS:
                    strMarketStatus = "On Auction no edits";
                    break;
                case dto.endpoint.type.MarketStatus.SUSPENDED:
                    strMarketStatus = "Suspended";
                    break;
                default:
                    strMarketStatus = "Closed";
                    break;
            }
            return strMarketStatus;
        }
      
    }

}
