using System.Collections.Generic;
using dto.endpoint.auth.session;

namespace dto.endpoint.application.operation
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

public class Application{
	///<Summary>
	///Application name
	///</Summary>
	public string name { get; set; }
	///<Summary>
	///API key
	///</Summary>
	public string apiKey { get; set; }
	///<Summary>
	///Application status
	///</Summary>
	public string status { get; set; }
	///<Summary>
	///Overall request per minute allowance
	///</Summary>
	public int allowanceApplicationOverall { get; set; }
	///<Summary>
	///Per account trading request per minute allowance
	///</Summary>
	public int allowanceAccountTrading { get; set; }
	///<Summary>
	///Per account request per minute allowance
	///</Summary>
	public int allowanceAccountOverall { get; set; }
	///<Summary>
	///Historical price data data points per minute allowance
	///</Summary>
	public int allowanceAccountHistoricalData { get; set; }
	///<Summary>
	///True if access to equity prices is permitted
	///</Summary>
	public bool allowEquities { get; set; }
	///<Summary>
	///True if quote orders are permitted
	///</Summary>
	public bool allowQuoteOrders { get; set; }
}
}
