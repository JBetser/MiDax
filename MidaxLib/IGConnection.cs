using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using dto.colibri.endpoint.auth.v2;
using IGPublicPcl;
using Lightstreamer.DotNet.Client;
using Newtonsoft.Json;

namespace MidaxLib
{
    public class IGMidaxStreamingApiClient : IGStreamingApiClient
    {
        public TimerCallback ConnectionClosed = null;

        public override void OnActivityWarning(bool warningOn)
        {
            Log.Instance.WriteEntry("Activity warning: " + warningOn, EventLogEntryType.Warning);
            base.OnActivityWarning(warningOn);
        }

        public override void OnClose()
        {
            Log.Instance.WriteEntry("Connection closed", EventLogEntryType.Information);
            base.OnClose();
            if (ConnectionClosed != null)
                ConnectionClosed(null);
        }

        public override void OnConnectionEstablished()
        {
            Log.Instance.WriteEntry("Connection established", EventLogEntryType.Information);
            base.OnConnectionEstablished();
        }

        public override void OnDataError(PushServerException e)
        {
            Log.Instance.WriteEntry("Data error: " + e.ToString(), EventLogEntryType.Error);
            base.OnDataError(e);
        }

        public override void OnEnd(int cause)
        {
            Log.Instance.WriteEntry("End, cause: " + cause, EventLogEntryType.Error);
            base.OnEnd(cause);
        }

        public override void OnFailure(PushConnException e)
        {
            Log.Instance.WriteEntry("Failure: " + e.ToString(), EventLogEntryType.Error);
            base.OnFailure(e);
        }

        public override void OnFailure(PushServerException e)
        {
            Log.Instance.WriteEntry("Failure: " + e.ToString(), EventLogEntryType.Error);
            base.OnFailure(e);
        }

        public override void OnSessionStarted(bool isPolling)
        {
            Log.Instance.WriteEntry("Session started, polling: " + isPolling, EventLogEntryType.Information);
            base.OnSessionStarted(isPolling);
        }
    }       

    public class IGTradingStreamingClient : IAbstractStreamingClient
    {
        public TimerCallback ConnectionClosed = null;

        IGMidaxStreamingApiClient _igStreamApiClient = new IGMidaxStreamingApiClient();
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
                            _igStreamApiClient.ConnectionClosed = ConnectionClosed;
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

        public override void Connect(TimerCallback connectionClosed)
        {
            try
            {
                ((IGTradingStreamingClient)_apiStreamingClient).ConnectionClosed = connectionClosed;
                _apiStreamingClient.Connect(Config.Settings["USER_NAME"], Config.Settings["PASSWORD"], Config.Settings["API_KEY"]);
            }
            catch (Exception ex)
            {
                Log.Instance.WriteEntry(ex.Message, EventLogEntryType.Error);
            }
        }
    }
}
