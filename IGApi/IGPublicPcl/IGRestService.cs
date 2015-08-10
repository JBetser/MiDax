using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Net;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using IGPublicPcl.Security;
using IgPublicPcl;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using Newtonsoft.Json.Serialization;
using dto.colibri.endpoint.encryptionkey;

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
    /// Unless required by applicable law or agreen to in writing, software
    /// distributed under the License is distributed on an 'AS IS' BASIS,
    /// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
    /// See the License for the specifi language governing permissions and
    /// limitations under the License.
    ///
    /// </Summary>
    public class ConversationContext
    {
        public ConversationContext(string cst, string xSecurityToken, string apiKey)
        {
            this.cst = cst;
            this.xSecurityToken = xSecurityToken;
            this.apiKey = apiKey;
        }

        public string cst;
        public string xSecurityToken;
        public string apiKey;
    }
  
    public class IgRestService
    {
        private static string _baseUrl = "https://demo-api.ig.com";   

        public IgRestService(string env = "demo-api")
        {
            _baseUrl = String.Format("https://{0}.ig.com", env);
        }               

        public void ParseHeaders(ConversationContext conversationContext, HttpResponseHeaders headers)
        {
            if (conversationContext != null)
            {
                foreach (var header in headers)
                {
                    if (header.Key == "CST")
                    {
                        conversationContext.cst = header.Value.First();
                    }
                    if (header.Key.Equals("X-SECURITY-TOKEN"))
                    {
                        conversationContext.xSecurityToken = header.Value.First();
                    }
                }
            }
        }

        public void SetDefaultRequestHeaders(HttpClient client,  ConversationContext conversationContext, string version)
        {
            if (conversationContext != null)
            {
                if (conversationContext.apiKey != null)
                {
                    client.DefaultRequestHeaders.Add("X-IG-API-KEY", conversationContext.apiKey);
                }
                if (conversationContext.cst != null)
                {
                    client.DefaultRequestHeaders.Add("CST", conversationContext.cst);
                }
                if (conversationContext.xSecurityToken != null)
                {
                    client.DefaultRequestHeaders.Add("X-SECURITY-TOKEN", conversationContext.xSecurityToken);
                }                             
                client.DefaultRequestHeaders.Add("VERSION", version);

            }          
            //This only works for version 1 !!!           
            //client.DefaultRequestHeaders.TryAddWithoutValidation("version", version ?? "1");
        }
       
        public async Task RestfulService(string uri, HttpMethod method, string version,
                                            ConversationContext conversationContext, Object bodyInput = null)
        {
            StringContent scontent;            

            if (bodyInput != null)
            {
                scontent = new StringContent(JsonConvert.SerializeObject(bodyInput));
                scontent.Headers.ContentType = new MediaTypeHeaderValue("application/json");
            }
            else
            {
                scontent = null;
            }

            var client = new HttpClient();
            var response = new HttpResponseMessage();

            SetDefaultRequestHeaders(client, conversationContext, version);
          
            switch (method.Method)
            {
                case "POST":
                    var postTask = client.PostAsync(_baseUrl + uri, scontent);
                    response = postTask.Result;                                     
                    break;

                case "GET":
                    var getTask = client.GetAsync(_baseUrl + uri);
                    response = getTask.Result;                                                
                    break;

                case "PUT":
                    var putTask = client.PutAsync(_baseUrl + uri, scontent);
                    response = putTask.Result;                            
                    break;

                case "DELETE":
                    var deleteTask = client.DeleteAsync(_baseUrl + uri);
                    response = deleteTask.Result;                                                
                    break;           
            }

            if (response != null)
            {
                ParseHeaders(conversationContext, response.Headers);
                await response.Content.ReadAsStringAsync();
            }
        }
            
        public async Task<IgResponse<T>> RestfulService<T>(string uri, HttpMethod method, string version,
                                               ConversationContext conversationContext, Object bodyInput = null)
        {           
            StringContent scontent;

            var localVar = new IgResponse<T> {Response = default(T), StatusCode = HttpStatusCode.OK}; 
                          
            if (bodyInput != null)
            {
                scontent = new StringContent(JsonConvert.SerializeObject(bodyInput));
                scontent.Headers.ContentType = new MediaTypeHeaderValue("application/json");
            }
            else
            {
                scontent = null;
            }
           
            var client = new HttpClient();
            SetDefaultRequestHeaders(client, conversationContext, version);

            var response = new HttpResponseMessage();

            string content = null;

            switch (method.Method)
            {
                case "POST":
                    var myPostTask = client.PostAsync(_baseUrl + uri, scontent);
                    response = myPostTask.Result;                                   
                    break;

                case "GET":
                    var myGetTask = client.GetAsync(_baseUrl + uri);
                    response = myGetTask.Result;                                 
                    break;

                case "PUT":
                    var myPutTask = client.PutAsync(_baseUrl + uri, scontent);
                    response = myPutTask.Result;                                    
                    break;

                case "DELETE":
                    var myDeleteTask = client.DeleteAsync(_baseUrl + uri);
                    response = myDeleteTask.Result;                                
                    break;
            }

            if (response != null)
            {
                ParseHeaders(conversationContext, response.Headers);
                localVar.StatusCode = response.StatusCode;
                if (localVar.StatusCode == HttpStatusCode.OK)
                {
                    content = await response.Content.ReadAsStringAsync();
                }
            }

            if (content != null)
            {
                var jss = new JsonSerializerSettings();
                jss.Converters.Add(new StringEnumConverter());
                jss.MissingMemberHandling = MissingMemberHandling.Ignore;
                jss.FloatFormatHandling = FloatFormatHandling.String;
                jss.NullValueHandling = NullValueHandling.Ignore;
                jss.Error += DeserializationError;
                client.Dispose();

                try
                {
                    var json = JsonConvert.DeserializeObject<T>(content, jss);
                    localVar.Response = json;
                }
                catch (Exception ex)
                {                    
                }
            }              
            return localVar;                           
        }
      
        private static void DeserializationError(object sender, ErrorEventArgs errorEventArgs)
        {            
            errorEventArgs.ErrorContext.Handled = true;                      
        }
    }   

}
