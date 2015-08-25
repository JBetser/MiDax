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

namespace MidaxLib
{
    public class IGTradingStreamingClient : IAbstractStreamingClient
    {
        IGStreamingApiClient _igStreamApiClient = new IGStreamingApiClient();
        IgRestApiClient _igRestApiClient = new IgRestApiClient();
        SubscribedTableKey _igSubscribedTableKey = null;
        string _currentAccount = null;

        public async void Connect(string username, string password, string apikey)
        {
            if (String.IsNullOrEmpty(apikey) || String.IsNullOrEmpty(password) ||
                String.IsNullOrEmpty(username))
            {
                Log.Instance.WriteEntry("Please enter API key, Password and Username", EventLogEntryType.Error);
                return;
            }

            // use v1 login...
            var ar = new AuthenticationRequest();
            ar.identifier = username;
            ar.password = password;

            //log in...
            var authenticationResponse = await _igRestApiClient.SecureAuthenticate(ar, apikey);

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

        void IAbstractStreamingClient.Subscribe(string[] epics, IHandyTableListener tableListener)
        {
            _igSubscribedTableKey = _igStreamApiClient.subscribeToMarketDetails(epics, tableListener);
        }

        void IAbstractStreamingClient.Unsubscribe()
        {
            _igStreamApiClient.UnsubscribeTableKey(_igSubscribedTableKey);
        }
    }

    public class IGConnection : MarketDataConnection
    {     
        public IGConnection()
        {
            _apiStreamingClient = new IGTradingStreamingClient();
        }

        public override void Connect()
        {
            try
            {
                _apiStreamingClient.Connect(Config.Settings["USER_NAME"], Config.Settings["PASSWORD"], Config.Settings["API_KEY"]);
            }
            catch (Exception ex)
            {
                Log.Instance.WriteEntry(ex.Message, EventLogEntryType.Error);
            }
        }
    }
}
