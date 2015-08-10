using System.Collections.Generic;
using dto.endpoint.auth.session;

namespace dto.endpoint.funds.manage
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

public class PaymentCard{
	///<Summary>
	///Card identifier
	///</Summary>
	public string id { get; set; }
	///<Summary>
	///Card type
	///</Summary>
	public string cardType { get; set; }
	///<Summary>
	///Last four digits of the card number
	///</Summary>
	public string presentationNumber { get; set; }
	///<Summary>
	///Cardholder name.
	///</Summary>
	public string holderName { get; set; }
	///<Summary>
	///Start date of the card in the format of mm/yy (may be null)
	///</Summary>
	public string startDate { get; set; }
	///<Summary>
	///Expiry date of the card in the format of mm/yy
	///</Summary>
	public string expiryDate { get; set; }
	///<Summary>
	///Currency
	///</Summary>
	public string currencyCode { get; set; }
	///<Summary>
	///Issue number of the card (may be null)
	///</Summary>
	public string issueNumber { get; set; }
	///<Summary>
	///</Summary>
	public bool fundsReceived { get; set; }
	///<Summary>
	///Minimum amount in the card currency which can be deposited to this card
	///</Summary>
	public decimal minimumAmountDeposit { get; set; }
	///<Summary>
	///Maximum amount in the card currency which can be deposited to this card
	///</Summary>
	public decimal maximumAmountDeposit { get; set; }
}
}
