using System.Collections.Generic;
using dto.endpoint.auth.session;

namespace dto.endpoint.workingorders.get.v1
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
	public decimal? size { get; set; }
	///<Summary>
	///Price at which to execute the trade
	///</Summary>
	public decimal? level { get; set; }
	///<Summary>
	///Working order expiry date and time. If set, format is dd/MM/yy HH:mm
	///</Summary>
	public string goodTill { get; set; }
	///<Summary>
	///Date and time when the order was created. Format is yyyy/MM/dd kk:mm:ss:SSS
	///</Summary>
	public string createdDate { get; set; }
	///<Summary>
	///True if controlled risk
	///</Summary>
	public bool controlledRisk { get; set; }
	///<Summary>
	///Trailing trigger increment
	///</Summary>
	public decimal? trailingTriggerIncrement { get; set; }
	///<Summary>
	///Trailing stop distance
	///</Summary>
	public decimal? trailingTriggerDistance { get; set; }
	///<Summary>
	///Trailing stop distance
	///</Summary>
	public decimal? trailingStopDistance { get; set; }
	///<Summary>
	///Trailing stop increment
	///</Summary>
	public decimal? trailingStopIncrement { get; set; }
	///<Summary>
	///Request type
	///</Summary>
	public string requestType { get; set; }
	///<Summary>
	///Stop level
	///</Summary>
	public decimal? contingentStop { get; set; }
	///<Summary>
	///Currency ISO code
	///</Summary>
	public string currencyCode { get; set; }
	///<Summary>
	///Limit level
	///</Summary>
	public decimal? contingentLimit { get; set; }
	///<Summary>
	///True if this is a DMA working order
	///</Summary>
	public bool dma { get; set; }
}
}
