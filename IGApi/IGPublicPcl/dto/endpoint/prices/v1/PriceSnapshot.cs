using System.Collections.Generic;
using dto.endpoint.auth.session;

namespace dto.endpoint.prices.v1
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

public class PriceSnapshot{
	///<Summary>
	///Snapshot time
	///</Summary>
	public string snapshotTime { get; set; }
	///<Summary>
	///Open price
	///</Summary>
	public Price openPrice { get; set; }
	///<Summary>
	///Close price
	///</Summary>
	public Price closePrice { get; set; }
	///<Summary>
	///High price
	///</Summary>
	public Price highPrice { get; set; }
	///<Summary>
	///Low price
	///</Summary>
	public Price lowPrice { get; set; }
	///<Summary>
	///Last traded volume. This will generally be 0 for non exchange-traded instruments
	///</Summary>
	public decimal? lastTradedVolume { get; set; }
}
}
