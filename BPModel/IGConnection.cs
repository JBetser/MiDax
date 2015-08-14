using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using dto.colibri.endpoint.auth.v2;
using IGPublicPcl;
using Lightstreamer.DotNet.Client;
using Newtonsoft.Json;

namespace BPModel
{
    public class IGConnection
    {
        IgRestApiClient _igRestApiClient = null;
        IGStreamingApiClient _igStreamApiClient = null;
        static IGConnection _instance = null;
        string _appName = null;
        string _apikey = null;
        string _userName = null;
        string _password = null;
        string _currentAccount = null;
        MarketDataSubscription _mktDataListener = null;        

        IGConnection(){}

        static public IGConnection Instance
        {
            get { return _instance == null ? _instance = new IGConnection() : _instance; }
        }

        public IgRestApiClient RestClient
        {
            get { return _igRestApiClient; }
        }

        public IGStreamingApiClient StreamClient
        {
            get { return _igStreamApiClient; }
        }
        
        public async void Init(string appname, string apikey, string username, string password)
        {
            try
            {
                _appName = appname;
                _apikey = apikey;
                _userName = username;
                _password = password;

                _igRestApiClient = new IgRestApiClient();
                _igStreamApiClient = new IGStreamingApiClient();
                _mktDataListener = new MarketDataSubscription();                

                if (String.IsNullOrEmpty(_apikey) || String.IsNullOrEmpty(_password) ||
                    String.IsNullOrEmpty(_userName) || (_igRestApiClient == null))
                {
                    Log.Instance.WriteEntry("Please enter API key, Password and Username", EventLogEntryType.Error);
                    return;
                }

                // use v1 login...
                var ar = new AuthenticationRequest();
                ar.identifier = _userName;
                ar.password = _password;

                //log in...
                var authenticationResponse = await _igRestApiClient.SecureAuthenticate(ar, _apikey);

                if (authenticationResponse && (authenticationResponse.Response != null) && (authenticationResponse.Response.accounts != null))
                {
                    if (authenticationResponse.Response.accounts.Count > 0)
                    {
                        Log.Instance.WriteEntry(JsonConvert.SerializeObject(authenticationResponse, Formatting.Indented), EventLogEntryType.Information);
                        Log.Instance.WriteEntry("Logged in, current account: " + authenticationResponse.Response.currentAccountId, EventLogEntryType.Information);

                        _currentAccount = authenticationResponse.Response.currentAccountId;

                        ConversationContext context = _igRestApiClient.GetConversationContext();

                        Log.Instance.WriteEntry("establishing streaming data connection", EventLogEntryType.Information);

                        if ((context != null) && (authenticationResponse.Response.currentAccountId != null) &&
                            (authenticationResponse.Response.lightstreamerEndpoint != null))
                        {
                            if (_igStreamApiClient != null)
                            {
                                var connectionEstablished =
                                    _igStreamApiClient.Connect(authenticationResponse.Response.currentAccountId, context.cst,
                                                               context.xSecurityToken, context.apiKey,
                                                               authenticationResponse.Response.lightstreamerEndpoint);
                                if (connectionEstablished)
                                {
                                    Log.Instance.WriteEntry("streaming data connection established", EventLogEntryType.Information);
                                }
                                else
                                {
                                    Log.Instance.WriteEntry("streaming data connection could NOT be established", EventLogEntryType.Error);
                                }
                            }
                        }
                        else
                        {
                            Log.Instance.WriteEntry("Could not establish streaming data connection.", EventLogEntryType.Error);
                        }
                    }
                    else
                    {
                        Log.Instance.WriteEntry("no accounts", EventLogEntryType.Error);
                    }
                }
                else
                {
                    Log.Instance.WriteEntry("Authentication Rest Response error : " + authenticationResponse.StatusCode, EventLogEntryType.Error);
                }
            }
            catch (Exception ex)
            {
                Log.Instance.WriteEntry(ex.Message, EventLogEntryType.Error);
            }
        }

        public void SubscribeMarketData(MarketData mktData)
        {
            _mktDataListener.MarketData.Add(mktData);
        }

        public void StartListening()
        {
            _mktDataListener.StartListening();
        }

        public void StopListening()
        {
            _mktDataListener.StopListening();
        } 
    }
}
