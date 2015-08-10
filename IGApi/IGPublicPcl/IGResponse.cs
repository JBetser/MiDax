using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace IgPublicPcl
{
    /// <Summary>
    ///
    /// IG API - IGResponse.cs
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
    public class IgResponse<T>
    {
        public HttpStatusCode StatusCode { get; set; }
        public T Response { get; set; }

        public static implicit operator bool(IgResponse<T> inst)
        {
            return inst.StatusCode == HttpStatusCode.OK;
        }

        public static implicit operator HttpStatusCode(IgResponse<T> inst)
        {
            return inst.StatusCode;
        }

        public static implicit operator T(IgResponse<T> inst)
        {
            return inst.Response;
        }
    }
}
