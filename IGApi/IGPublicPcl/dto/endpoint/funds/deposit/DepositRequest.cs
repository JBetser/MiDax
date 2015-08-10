using System.Collections.Generic;
using dto.endpoint.auth.session;

namespace dto.endpoint.funds.deposit
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

public class DepositRequest{
	///<Summary>
	///Source card identifier
	///</Summary>
	public string cardId { get; set; }
	///<Summary>
	///Card cw2 number
	///</Summary>
	public string cvv2 { get; set; }
	///<Summary>
	///Client password
	///</Summary>
	public char[] password { get; set; }
	///<Summary>
	///Deposit amount
	///</Summary>
	public decimal amount { get; set; }
}
}
