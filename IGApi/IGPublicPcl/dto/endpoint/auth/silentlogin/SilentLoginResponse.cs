using System.Collections.Generic;
using dto.endpoint.auth.session;

namespace dto.endpoint.auth.silentlogin
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

public class SilentLoginResponse{
	///<Summary>
	///Account type
	///</Summary>
	public string accountType { get; set; }
	///<Summary>
	///Active account summary
	///</Summary>
	public AccountInfo accountInfo { get; set; }
	///<Summary>
	///Account currency
	///</Summary>
	public string currencyIsoCode { get; set; }
	///<Summary>
	///Active account identifier
	///</Summary>
	public string currentAccountId { get; set; }
	///<Summary>
	///Whether the Client has active demo accounts.
	///</Summary>
	public bool hasActiveDemoAccounts { get; set; }
	///<Summary>
	///Whether the Client has active live accounts.
	///</Summary>
	public bool hasActiveLiveAccounts { get; set; }
	///<Summary>
	///Lightstreamer endpoint for subscribing to account and price updates
	///</Summary>
	public string lightstreamerEndpoint { get; set; }
	///<Summary>
	///Client account summaries
	///</Summary>
	public List<AccountDetails> accounts { get; set; }
	///<Summary>
	///Client identifier
	///</Summary>
	public string clientId { get; set; }
}
}
