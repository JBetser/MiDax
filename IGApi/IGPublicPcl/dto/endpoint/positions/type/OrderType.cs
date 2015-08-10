using System.Collections.Generic;
using dto.endpoint.auth.session;

namespace dto.endpoint.positions.type
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

	public enum OrderType {


   	///<Summary>
	///Quote orders get executed at the specified level.
	///The level has to be accompanied by a valid quote id. This type is only available subject to agreement with IG.
	///</Summary>

   QUOTE,

   	///<Summary>
	///Market orders get executed at the price seen by the IG at the time of booking the trade.
	///A level cannot be specified. Not applicable to BINARY instruments
	///</Summary>

   MARKET,

   	///<Summary>
	///Fill or Kill Limit order, i.e. a market order where the limit determines the price at which to kill the order
	///</Summary>

   LIMIT,}

}
