using System.Collections.Generic;
using dto.endpoint.auth.session;

namespace dto.endpoint.marketdetails.v1
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

public class DealingRulesData{
	///<Summary>
	///Minimum step distance
	///</Summary>
	public DealingRuleData minStepDistance { get; set; }
	///<Summary>
	///Minimum deal size
	///</Summary>
	public DealingRuleData minDealSize { get; set; }
	///<Summary>
	///Minimum controlled risk stop distance
	///</Summary>
	public DealingRuleData minControlledRiskStopDistance { get; set; }
	///<Summary>
	///Minimum stop or limit distance
	///</Summary>
	public DealingRuleData minNormalStopOrLimitDistance { get; set; }
	///<Summary>
	///Maximum stop or limit distance
	///</Summary>
	public DealingRuleData maxStopOrLimitDistance { get; set; }
	///<Summary>
	///The client's market order preference for creating or closing positions.
	///This should be ignored when editing positions and when creating, editing and deleting working orders.
	///</Summary>
	public string marketOrderPreference { get; set; }
}
}
