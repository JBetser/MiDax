using System.Collections.Generic;
using dto.endpoint.auth.session;

namespace dto.endpoint.positions.create.otc.v2
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

public class CreatePositionRequest{
	///<Summary>
	///Instrument epic identifier
	///</Summary>
	public string epic { get; set; }
	///<Summary>
	///Instrument expiry
	///</Summary>
	public string expiry { get; set; }
	///<Summary>
	///Deal direction
	///</Summary>
	public string direction { get; set; }
	///<Summary>
	///Deal size
	///</Summary>
	public decimal? size { get; set; }
	///<Summary>
	///Deal level
	///</Summary>
	public decimal? level { get; set; }
	///<Summary>
	///Order type
	///</Summary>
	public string orderType { get; set; }
	///<Summary>
	///True if a guaranteed stop is required
	///</Summary>
	public bool guaranteedStop { get; set; }
	///<Summary>
	///Stop level
	///</Summary>
	public decimal? stopLevel { get; set; }
	///<Summary>
	///Stop distance
	///</Summary>
	public decimal? stopDistance { get; set; }
	///<Summary>
	///Whether the stop has to be moved towards the current level in case of a favourable trade
	///</Summary>
	public bool trailingStop { get; set; }
	///<Summary>
	///increment step in pips for the trailing stop
	///</Summary>
	public decimal? trailingStopIncrement { get; set; }
	///<Summary>
	///True if force open is required
	///</Summary>
	public bool forceOpen { get; set; }
	///<Summary>
	///Limit level
	///</Summary>
	public decimal? limitLevel { get; set; }
	///<Summary>
	///Limit distance
	///</Summary>
	public decimal? limitDistance { get; set; }
	///<Summary>
	///Lightstreamer price quote identifier
	///</Summary>
	public string quoteId { get; set; }
	///<Summary>
	///Currency. Restricted to available instrument currencies
	///</Summary>
	public string currencyCode { get; set; }
}
}
