using System.Collections.Generic;
using dto.endpoint.auth.session;

namespace dto.endpoint.positions.close.v1
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

public class ClosePositionRequest{
	///<Summary>
	///Deal identifier
	///</Summary>
	public string dealId { get; set; }
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
	///Closing deal level
	///</Summary>
	public decimal? level { get; set; }
	///<Summary>
	///True if a market order is required
	///</Summary>
	public string orderType { get; set; }
	///<Summary>
	///Lightstreamer price quote identifier
	///</Summary>
	public string quoteId { get; set; }
}
}
