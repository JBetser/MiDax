using System.Collections.Generic;
using dto.endpoint.auth.session;

namespace dto.endpoint.prices.v2
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

public class PriceList{
	///<Summary>
	///Price list
	///</Summary>
	public List<PriceSnapshot> prices { get; set; }
	///<Summary>
	///the instrument type of this instrument
	///</Summary>
	public string instrumentType { get; set; }
	///<Summary>
	///Historical price data allowance
	///</Summary>
	public Allowance allowance { get; set; }
}
}
