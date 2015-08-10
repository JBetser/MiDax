using System.Collections.Generic;
using dto.endpoint.auth.session;

namespace dto.endpoint.accountactivity.transaction
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

public class Transaction{
	///<Summary>
	///Transaction date, format dd-MMM-yyyy
	///</Summary>
	public string date { get; set; }
	///<Summary>
	///Instrument name
	///</Summary>
	public string instrumentName { get; set; }
	///<Summary>
	///Period in milliseconds
	///</Summary>
	public string period { get; set; }
	///<Summary>
	///Profit and loss
	///</Summary>
	public string profitAndLoss { get; set; }
	///<Summary>
	///Transaction type
	///</Summary>
	public string transactionType { get; set; }
	///<Summary>
	///Reference
	///</Summary>
	public string reference { get; set; }
	///<Summary>
	///Level at which the order was opened
	///</Summary>
	public string openLevel { get; set; }
	///<Summary>
	///Level at which the order was closed
	///</Summary>
	public string closeLevel { get; set; }
	///<Summary>
	///Formatted order size, including the direction (+ for buy, - for sell)
	///</Summary>
	public string size { get; set; }
	///<Summary>
	///Order currency
	///</Summary>
	public string currency { get; set; }
	///<Summary>
	///True if this was a cash transaction
	///</Summary>
	public bool cashTransaction { get; set; }
}
}
