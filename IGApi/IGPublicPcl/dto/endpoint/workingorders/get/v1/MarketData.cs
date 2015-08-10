using System.Collections.Generic;
using dto.endpoint.auth.session;

namespace dto.endpoint.workingorders.get.v1
{
   /// <Summary>
   ///
   /// IG API - Sample Client
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
   /// </Summary>

public class MarketData{
	///<Summary>
	///Instrument name
	///</Summary>
	public string instrumentName { get; set; }
	///<Summary>
	///Exchange identifier for this instrument
	///</Summary>
	public string exchangeId { get; set; }
	///<Summary>
	///Instrument expiry period
	///</Summary>
	public string expiry { get; set; }
	///<Summary>
	///</Summary>
	public string marketStatus { get; set; }
	///<Summary>
	///Instrument epic identifier
	///</Summary>
	public string epic { get; set; }
	///<Summary>
	///Instrument type
	///</Summary>
	public string instrumentType { get; set; }
	///<Summary>
	///Instrument lot size
	///</Summary>
	public decimal? lotSize { get; set; }
	///<Summary>
	///High price
	///</Summary>
	public decimal? high { get; set; }
	///<Summary>
	///Low price
	///</Summary>
	public decimal? low { get; set; }
	///<Summary>
	///Price percentage change
	///</Summary>
	public decimal? percentageChange { get; set; }
	///<Summary>
	///Price net change
	///</Summary>
	public decimal? netChange { get; set; }
	///<Summary>
	///Bid
	///</Summary>
	public decimal? bid { get; set; }
	///<Summary>
	///Offer
	///</Summary>
	public decimal? offer { get; set; }
	///<Summary>
	///Last instrument price update time
	///</Summary>
	public string updateTime { get; set; }
	///<Summary>
	///Instrument price delay (minutes)
	///</Summary>
	public int delayTime { get; set; }
	///<Summary>
	///True if streaming prices are available
	///</Summary>
	public bool streamingPricesAvailable { get; set; }
	///<Summary>
	///multiplying factor to determine actual pip value for the levels used by the instrument
	///</Summary>
	public int scalingFactor { get; set; }
}
}
