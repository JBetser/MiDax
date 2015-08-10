using System.Collections.Generic;
using dto.endpoint.auth.session;

namespace dto.endpoint.workingorders.create.v2
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

public class CreateWorkingOrderRequest{
	///<Summary>
	///Instrument epic
	///</Summary>
	public string epic { get; set; }
	///<Summary>
	///Expiry
	///</Summary>
	public string expiry { get; set; }
	///<Summary>
	///Deal direction
	///</Summary>
	public string direction { get; set; }
	///<Summary>
	///Order size
	///</Summary>
	public decimal? size { get; set; }
	///<Summary>
	///Deal level
	///</Summary>
	public decimal? level { get; set; }
	///<Summary>
	///Deal type
	///</Summary>
	public string type { get; set; }
	///<Summary>
	///Currency.  Restricted to available instrument currencies
	///</Summary>
	public string currencyCode { get; set; }
	///<Summary>
	///Time in force type
	///</Summary>
	public string timeInForce { get; set; }
	///<Summary>
	///Good till date - format is yyyy/mm/dd hh:mm:ss
	///</Summary>
	public string goodTillDate { get; set; }
	///<Summary>
	///Guaranteed stop
	///</Summary>
	public bool guaranteedStop { get; set; }
	///<Summary>
	///Stop distance
	///</Summary>
	public decimal? stopDistance { get; set; }
	///<Summary>
	///Limit distance
	///</Summary>
	public decimal? limitDistance { get; set; }
}
}
