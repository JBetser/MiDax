using System.Collections.Generic;
using dto.endpoint.auth.session;

namespace dto.endpoint.confirms
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

public class ConfirmsResponse{
	///<Summary>
	///Position status
	///</Summary>
	public string status { get; set; }
	///<Summary>
	///</Summary>
	public string reason { get; set; }
	///<Summary>
	///Deal status
	///</Summary>
	public string dealStatus { get; set; }
	///<Summary>
	///Instrument epic identifier
	///</Summary>
	public string epic { get; set; }
	///<Summary>
	///Instrument expiry
	///</Summary>
	public string expiry { get; set; }
	///<Summary>
	///Deal reference
	///</Summary>
	public string dealReference { get; set; }
	///<Summary>
	///Deal identifier
	///</Summary>
	public string dealId { get; set; }
	///<Summary>
	///List of affected deals
	///</Summary>
	public List<AffectedDeal> affectedDeals { get; set; }
	///<Summary>
	///Level
	///</Summary>
	public decimal? level { get; set; }
	///<Summary>
	///Size
	///</Summary>
	public decimal? size { get; set; }
	///<Summary>
	///Direction
	///</Summary>
	public string direction { get; set; }
	///<Summary>
	///Stop level
	///</Summary>
	public decimal? stopLevel { get; set; }
	///<Summary>
	///Limit level
	///</Summary>
	public decimal? limitLevel { get; set; }
	///<Summary>
	///Stop distance
	///</Summary>
	public decimal? stopDistance { get; set; }
	///<Summary>
	///Limit distance
	///</Summary>
	public decimal? limitDistance { get; set; }
	///<Summary>
	///True if guaranteed stop
	///</Summary>
	public bool guaranteedStop { get; set; }
}
}
