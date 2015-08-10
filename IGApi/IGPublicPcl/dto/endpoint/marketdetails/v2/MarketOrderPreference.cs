using System.Collections.Generic;
using dto.endpoint.auth.session;

namespace dto.endpoint.marketdetails.v2
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

	public enum MarketOrderPreference {


   	///<Summary>
	///Market orders are not allowed for the current site and/or instrument
	///</Summary>

   NOT_AVAILABLE,

   	///<Summary>
	///Market orders are allowed for the account type and instrument, and the user has enabled market orders in their preferences but decided the default state is off.
	///</Summary>

   AVAILABLE_DEFAULT_OFF,

   	///<Summary>
	///Market orders are allowed for the account type and instrument, and the user has enabled market orders in their preferences and has decided the default state is on.
	///</Summary>

   AVAILABLE_DEFAULT_ON,}

}
