using System.Collections.Generic;
using dto.endpoint.auth.session;

namespace dto.endpoint.positions.create.sprintmarkets.v1
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

public class CreateSprintMarketPosition{
	///<Summary>
	///</Summary>
	public string epic { get; set; }
	///<Summary>
	///</Summary>
	public string expiry { get; set; }
	///<Summary>
	///Order Type for the trade. Currently the only allowed type is QUOTE
	///</Summary>
	public string orderType { get; set; }
	///<Summary>
	///</Summary>
	public string currencyCode { get; set; }
	///<Summary>
	///</Summary>
	public string direction { get; set; }
	///<Summary>
	///</Summary>
	public decimal? size { get; set; }
	///<Summary>
	///</Summary>
	public decimal? strikeLevel { get; set; }
	///<Summary>
	///</Summary>
	public decimal? binaryOdds { get; set; }
	///<Summary>
	///</Summary>
	public string expiryTime { get; set; }
	///<Summary>
	///</Summary>
	public decimal? quoteId { get; set; }
}
}
