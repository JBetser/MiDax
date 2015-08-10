using System.Collections.Generic;
using dto.endpoint.auth.session;

namespace dto.endpoint.funds.manage.type
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

public class AddEditCardResponse{
	public enum Status {

      	///<Summary>
	///Success
	///</Summary>

      SUCCESS,
      	///<Summary>
	///Invalid card details
	///</Summary>

      DETAILS_INVALID,
      	///<Summary>
	///Card not supported
	///</Summary>

      CARD_NOT_SUPPORTED,
      	///<Summary>
	///Account cannot add card
	///</Summary>

      ACCOUNT_CANNOT_ADD_CARD,
      	///<Summary>
	///Card number limit reached
	///</Summary>

      CARD_NUMBER_LIMIT_REACHED,
      	///<Summary>
	///General failure
	///</Summary>

      FAILURE,}
	///<Summary>
	///Status
	///</Summary>
	public string status { get; set; }
}
}
