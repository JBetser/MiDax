using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using IGPublicPcl.Security;
using IgPublicPcl;
using PCLCrypto;
using dto.colibri.endpoint.encryptionkey;
using dto.colibri.endpoint.auth.v2;

namespace IGPublicPcl
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
   
	public partial class IgRestApiClient
	{
		private ConversationContext _conversationContext;

		private IgRestService _igRestService = new IgRestService();       

        private EncryptionKeyResponse ekr { get; set; }

        public ConversationContext GetConversationContext()
	    {
			return _conversationContext;
		}

		public async Task<IgResponse<AuthenticationResponse>> Authenticate(AuthenticationRequest ar, string apiKey)
        {
            _conversationContext = new ConversationContext(null, null, apiKey);                    
            return await authenticate(ar);
        }

		public async Task<IgResponse<AuthenticationResponse>> SecureAuthenticate(AuthenticationRequest ar, string apiKey)
        {
            _conversationContext = new ConversationContext(null, null, apiKey);           
            var encryptedPassword = await SecurePassword(ar.password);

            if (encryptedPassword == ar.password)
            {               
               ar.encryptedPassword = false;
            }
            else               
            {
                ar.encryptedPassword = true;                
            }
            ar.password = encryptedPassword;
            return await authenticate(ar);
        }	

        private async Task<string> SecurePassword(string rawPassword)
        {           
            var encryptedPassword = rawPassword;
          

            //Try encrypting password. If we can encrypt it, do so...                                                                            
            var secureResponse = await fetchEncryptionKey();
          
            ekr = new EncryptionKeyResponse();
            ekr = secureResponse.Response;
           
            if (ekr != null)
            {
                byte[] encryptedBytes;

                // get a public key to ENCRYPT...
                Rsa rsa = new Rsa(Convert.FromBase64String(ekr.encryptionKey), true);

                encryptedBytes = rsa.RsaEncrypt(string.Format("{0}|{1}", rawPassword, ekr.timeStamp));
                encryptedPassword = Convert.ToBase64String(encryptedBytes);
            }
            return encryptedPassword;            
        }      
	}
}
