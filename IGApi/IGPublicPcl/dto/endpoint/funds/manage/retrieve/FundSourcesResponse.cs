using System.Collections.Generic;
using dto.endpoint.auth.session;

namespace dto.endpoint.funds.manage.retrieve
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

public class FundSourcesResponse{
	///<Summary>
	///List of payment cards
	///</Summary>
	public List<PaymentCard> paymentCards { get; set; }
	///<Summary>
	///Maximum number of payment cards
	///todo need this?
	///</Summary>
	public int maxNumberOfPaymentCards { get; set; }
	///<Summary>
	///List of available currencies
	///</Summary>
	public List<string> availableCurrencies { get; set; }
}
}
