using System.Collections.Generic;
using dto.endpoint.auth.session;

namespace dto.endpoint.workingorders.get.v2
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

public class WorkingOrderData{
	///<Summary>
	///Deal identifier
	///</Summary>
	public string dealId { get; set; }
	///<Summary>
	///Deal direction
	///</Summary>
	public string direction { get; set; }
	///<Summary>
	///Instrument epic identifier
	///</Summary>
	public string epic { get; set; }
	///<Summary>
	///Order size
	///</Summary>
	public decimal? orderSize { get; set; }
	///<Summary>
	///Price at which to execute the trade
	///</Summary>
	public decimal? orderLevel { get; set; }
	///<Summary>
	///Time in force for this order
	///</Summary>
	public string timeInForce { get; set; }
	///<Summary>
	///The date and time the working order will be deleted if not triggered till then.
	///Date format is yyyy/MM/dd hh:mm
	///</Summary>
	public string goodTillDate { get; set; }
	///<Summary>
	///Date and time when the order was created. Format is yyyy/MM/dd kk:mm:ss:SSS
	///</Summary>
	public string createdDate { get; set; }
	///<Summary>
	///True if controlled risk
	///</Summary>
	public bool guaranteedStop { get; set; }
	///<Summary>
	///Request type
	///</Summary>
	public string orderType { get; set; }
	///<Summary>
	///Stop distance
	///</Summary>
	public decimal? stopDistance { get; set; }
	///<Summary>
	///Limit distance
	///</Summary>
	public decimal? limitDistance { get; set; }
	///<Summary>
	///Currency ISO code
	///</Summary>
	public string currencyCode { get; set; }
	///<Summary>
	///True if this is a DMA working order
	///</Summary>
	public bool dma { get; set; }
}
}
