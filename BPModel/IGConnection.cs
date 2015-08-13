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
        EventLog _logMgr = null;
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

        public EventLog Log
        {
            get { return _logMgr; }
        }

        public async void Init(string appname, string apikey, string username, string password)
        {
            try
            {
                _appName = appname;
                _apikey = apikey;
                _userName = username;
                _password = password;                
                if (!EventLog.SourceExists(_appName))
                    EventLog.CreateEventSource(new EventSourceCreationData(_appName, "BPModel"));
                _logMgr = new EventLog("BPModel", Environment.MachineName, _appName);
                _logMgr.WriteEntry("Attempting login", EventLogEntryType.Information);

                _igRestApiClient = new IgRestApiClient();
                _igStreamApiClient = new IGStreamingApiClient();
                _mktDataListener = new MarketDataSubscription();                

                if (String.IsNullOrEmpty(_apikey) || String.IsNullOrEmpty(_password) ||
                    String.IsNullOrEmpty(_userName) || (_igRestApiClient == null))
                {
                    _logMgr.WriteEntry("Please enter API key, Password and Username", EventLogEntryType.Error);
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
                        _logMgr.WriteEntry(JsonConvert.SerializeObject(authenticationResponse, Formatting.Indented), EventLogEntryType.Information);
                        _logMgr.WriteEntry("Logged in, current account: " + authenticationResponse.Response.currentAccountId, EventLogEntryType.Information);

                        _currentAccount = authenticationResponse.Response.currentAccountId;

                        ConversationContext context = _igRestApiClient.GetConversationContext();

                        _logMgr.WriteEntry("establishing streaming data connection", EventLogEntryType.Information);

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
                                    _logMgr.WriteEntry("streaming data connection established", EventLogEntryType.Information);
                                }
                                else
                                {
                                    _logMgr.WriteEntry("streaming data connection could NOT be established", EventLogEntryType.Error);
                                }
                            }
                        }
                        else
                        {
                            _logMgr.WriteEntry("Could not establish streaming data connection.", EventLogEntryType.Error);
                        }
                    }
                    else
                    {
                        _logMgr.WriteEntry("no accounts", EventLogEntryType.Error);
                    }
                }
                else
                {
                    _logMgr.WriteEntry("Authentication Rest Response error : " + authenticationResponse.StatusCode, EventLogEntryType.Error);
                }
            }
            catch (Exception ex)
            {
                _logMgr.WriteEntry(ex.Message, EventLogEntryType.Error);
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
