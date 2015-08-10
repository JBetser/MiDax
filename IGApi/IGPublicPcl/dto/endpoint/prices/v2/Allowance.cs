using System.Collections.Generic;
using dto.endpoint.auth.session;

namespace dto.endpoint.prices.v2
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

public class Allowance{
	///<Summary>
	///The number of data points still available to fetch within the current allowance period
	///</Summary>
	public int remainingAllowance { get; set; }
	///<Summary>
	///The number of data points the API key and account combination is allowed to fetch in any given allowance period
	///</Summary>
	public int totalAllowance { get; set; }
	///<Summary>
	///The number of seconds till the current allowance period will end and the remaining allowance field is reset
	///</Summary>
	public int allowanceExpiry { get; set; }
}
}
