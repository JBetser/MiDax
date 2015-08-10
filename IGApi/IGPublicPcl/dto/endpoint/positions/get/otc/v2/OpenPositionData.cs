using System.Collections.Generic;
using dto.endpoint.auth.session;

namespace dto.endpoint.positions.get.otc.v2
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

public class OpenPositionData{
	///<Summary>
	///Size of the contract
	///</Summary>
	public decimal? contractSize { get; set; }
	///<Summary>
	///Date the position was opened
	///</Summary>
	public string createdDate { get; set; }
	///<Summary>
	///Deal identifier
	///</Summary>
	public string dealId { get; set; }
	///<Summary>
	///Deal size
	///</Summary>
	public decimal? size { get; set; }
	///<Summary>
	///Deal direction
	///</Summary>
	public string direction { get; set; }
	///<Summary>
	///Limit level
	///</Summary>
	public decimal? limitLevel { get; set; }
	///<Summary>
	///Level at which the position was opened
	///</Summary>
	public decimal? level { get; set; }
	///<Summary>
	///Position currency ISO code
	///</Summary>
	public string currency { get; set; }
	///<Summary>
	///True if position is risk controlled
	///</Summary>
	public bool controlledRisk { get; set; }
	///<Summary>
	///Stop level
	///</Summary>
	public decimal? stopLevel { get; set; }
	///<Summary>
	///Trailing step size
	///</Summary>
	public decimal? trailingStep { get; set; }
	///<Summary>
	///Trailing stop distance
	///</Summary>
	public decimal? trailingStopDistance { get; set; }
}
}
