using System.Collections.Generic;
using dto.endpoint.auth.session;

namespace dto.endpoint.auth.session
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

public class AuthenticationResponse{
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
	///Account currency symbol
	///</Summary>
	public string currencySymbol { get; set; }
	///<Summary>
	///Active account identifier
	///</Summary>
	public string currentAccountId { get; set; }
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
	///<Summary>
	///Client account timezone offset relative to UTC, expressed in hours
	///</Summary>
	public int timezoneOffset { get; set; }
	///<Summary>
	///Whether the Client has active demo accounts.
	///</Summary>
	public bool hasActiveDemoAccounts { get; set; }
	///<Summary>
	///Whether the Client has active live accounts.
	///</Summary>
	public bool hasActiveLiveAccounts { get; set; }
	///<Summary>
	///Whether the account is allowed to set trailing stops on his trades
	///</Summary>
	public bool trailingStopsEnabled { get; set; }
	///<Summary>
	///If specified, indicates that the authentication process requires the client to switch to a different URL in order to complete the login.
	///If null, no rerouting has to take place and the authentication process is complete.
	///This is expected for any DEMO clients, where the authentication process is initiated against the production servers (i.e. https://api.ig.com/gateway/deal )
	///whereas all subsequent requests have to be issued against the DEMO servers (i.e. https://demo-api.ig.com/gateway/deal )
	///Please also note that when rerouting to DEMO it is also required to invoke to "silent login" endpoint in DEMO with the CST token
	///obtained by the preceding LIVE authentication endpoint invocation.
	///Please consult the http://labs.ig.com site for more details about the login rerouting details.
	///</Summary>
	public string reroutingEnvironment { get; set; }
	///<Summary>
	///</Summary>
	public string igCompany { get; set; }
	///<Summary>
	///</Summary>
	public string kycFormUrl { get; set; }
}
}
