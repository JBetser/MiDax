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

public class AccountDetails{
	///<Summary>
	///Account identifier
	///</Summary>
	public string accountId { get; set; }
	///<Summary>
	///Account name
	///</Summary>
	public string accountName { get; set; }
	///<Summary>
	///Indicates whether this account is the client's preferred account
	///</Summary>
	public bool preferred { get; set; }
	///<Summary>
	///Account type
	///</Summary>
	public string accountType { get; set; }
}
}
