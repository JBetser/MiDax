using System.Collections.Generic;
using dto.endpoint.auth.session;

namespace dto.endpoint.accountbalance
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

public class AccountDetails{
	public enum AccountStatus {

      	///<Summary>
	///Enabled
	///</Summary>

      ENABLED,
      	///<Summary>
	///Disabled
	///</Summary>

      DISABLED,
      	///<Summary>
	///Suspended from dealing
	///</Summary>

      SUSPENDED_FROM_DEALING,}
	public enum AccountType {

      	///<Summary>
	///CFD account
	///</Summary>

      CFD,
      	///<Summary>
	///Spread bet account
	///</Summary>

      SPREADBET,
      	///<Summary>
	///Physical account
	///</Summary>

      PHYSICAL,}
	///<Summary>
	///Account identifier
	///</Summary>
	public string accountId { get; set; }
	///<Summary>
	///Account name
	///</Summary>
	public string accountName { get; set; }
	///<Summary>
	///Account alias
	///</Summary>
	public string accountAlias { get; set; }
	///<Summary>
	///Account status
	///</Summary>
	public string status { get; set; }
	///<Summary>
	///Account type
	///</Summary>
	public string accountType { get; set; }
	///<Summary>
	///True if this the default login account
	///</Summary>
	public bool preferred { get; set; }
	///<Summary>
	///Account balances
	///</Summary>
	public AccountBalance balance { get; set; }
	///<Summary>
	///Account currency
	///</Summary>
	public string currency { get; set; }
	///<Summary>
	///True if account can be transferred to
	///</Summary>
	public bool canTransferFrom { get; set; }
	///<Summary>
	///True if account can be transferred from
	///</Summary>
	public bool canTransferTo { get; set; }
}
}
