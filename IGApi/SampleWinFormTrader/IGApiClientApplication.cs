using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Forms;
using dto.colibri.endpoint.auth.v2;
using IGPublicPcl;
using Lightstreamer.DotNet.Client;
using Newtonsoft.Json;
using dto.endpoint.positions.get.otc.v1;
using dto.endpoint.watchlists.retrieve;
using dto.endpoint.workingorders.get.v1;
 
namespace IgApiClientApplication 
{
    /// <Summary>
    ///
    /// IG Trader WinForms Sample Application 
    /// 
    /// WatchlistsViewModel
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
    public partial class DemoDotNetClientForm : Form 
    {
        private IgRestApiClient     _igRestApiClient ;
        private IGStreamingApiClient _igStreamApiClient;
      
        //
        // The 3 lightstreamer subscriptions referred to at http://labs.ig.com/streaming-api-reference
        //
        private AccountBalanceSubscription _accountBalanceSubscription;
        private TradeConfirmsSubscription _tradeConfirmsSubscription;

        private PositionSubscription _positionSubscription;
        private OrderSubscription _orderSubscription;
        private WatchlistMarketSubscription _watchlistItemSubscription;

        //
        // Subscription table keys, used to keep track of subscriptions...
        //
        private SubscribedTableKey _accountsStk;
        private SubscribedTableKey _tradeSubscriptionStk;
        private SubscribedTableKey _positionSubscriptionStk;
        private SubscribedTableKey _orderSubscriptionStk;
        private SubscribedTableKey _watchlistItemSubscriptionStk;       

        private List<Watchlist> _watchlists;
        private List<WatchlistMarket> _watchlistMarkets;
        private List<WorkingOrder> _orders;
        private List<OpenPosition> _positions; 
 
        private static DemoDotNetClientForm _myForm;

        private Dictionary<string,string> _accountIds;

        private string _currentAccount;

        private string _APIKey;

        delegate void SetTextCallback(string text);

        static void Main()
        {                 
           _myForm = new DemoDotNetClientForm();                    
           _myForm.ShowDialog();                                    
        }

        static void OnClose()
        {
            _myForm.Dispose();
        }

        public DemoDotNetClientForm() 
        {
            InitializeComponent();                      

            _accountIds = new Dictionary<string, string>();
            _accountsStk = new SubscribedTableKey();
           
            EnableCommandButtons(false);

            _watchlists     = new List<Watchlist>();
            _watchlistMarkets    = new List<WatchlistMarket>();
            _orders         = new List<WorkingOrder>();
            _positions      = new List<OpenPosition>();

            cbPositions.Enabled         = false;
            cbOrders.Enabled            = false;
            cbWatchlistItems.Enabled    = false;

            _igRestApiClient = new IgRestApiClient();
            _igStreamApiClient = new IGStreamingApiClient();

            _APIKey = "8d341413c2eae2c35bb5b47a594ef08ae18cb3b7";                 // *** TODO ENTER YOUR API KEY HERE ***
            passwordTextbox.Text = "Kotik0483";    // *** TODO Enter your password here ***
            identifierTextbox.Text = "ksbitlsoftdemo";  // *** TODO Enter your user name here ***            
        }
    
        public void EnableCommandButtons(bool state)
        {
            workingOrdersButton.Enabled = state;
            positionsButton.Enabled = state;
            watchlistsButton.Enabled = state;
            btnAccountDetails.Enabled = state;               
            btnMarketData.Enabled = state;
            btnLogout.Enabled = state;
            searchButton.Enabled = state;

            cbTradeSubscription.Enabled = state;
            cbAccountSubscription.Enabled = state;            
        }

        ///<Summary>
        ///loginButton_Click 
        ///This method deals with logging into the app.
        ///</Summary>
        private async void loginButton_Click(object sender, EventArgs e) 
        {
            try
            {
                AppendActivityMessage("Attempting login");

                if (String.IsNullOrEmpty(_APIKey) || String.IsNullOrEmpty(passwordTextbox.Text) ||
                    String.IsNullOrEmpty(identifierTextbox.Text) || (_igRestApiClient == null))
                {
                    AppendActivityMessage("Please enter API key, Password and Username");
                    return;
                }
               
				// use v1 login...
				var ar = new AuthenticationRequest();
                ar.identifier   = identifierTextbox.Text;
                ar.password     = passwordTextbox.Text;               

                //log in...
                var authenticationResponse = await _igRestApiClient.SecureAuthenticate(ar, _APIKey);               
              
                if (authenticationResponse && (authenticationResponse.Response != null) && (authenticationResponse.Response.accounts != null))
                {
                    if (authenticationResponse.Response.accounts.Count > 0)
                    {
                        AppendRestDataMessage(JsonConvert.SerializeObject(authenticationResponse, Formatting.Indented));
                        AppendActivityMessage("Logged in, current account: " + authenticationResponse.Response.currentAccountId);

                        _currentAccount = authenticationResponse.Response.currentAccountId;

                        ConversationContext context = _igRestApiClient.GetConversationContext();

                        AppendActivityMessage("establishing streaming data connection");

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
                                    AppendActivityMessage("streaming data connection established");
                                    EnableCommandButtons(true);
                                }
                                else
                                {
                                    AppendActivityMessage("streaming data connection could NOT be established");
                                    EnableCommandButtons(false);
                                }
                            }
                        }
                        else
                        {
                            AppendActivityMessage("Could not establish streaming data connection.");
                        }
                    }
                    else
                    {
                        AppendActivityMessage("no accounts");
                    }
                }
                else
                {
                    AppendActivityMessage("Authentication Rest Response error : " + authenticationResponse.StatusCode );    
                }                                  
            }
            catch (Exception ex)
            {
                AppendActivityMessage(ex.Message);
            }
        }

        private async void positionsButton_Click(object sender, EventArgs e)
        {            
            if (_igRestApiClient != null)
            {
                try
                {
                    var positionsResponse = await _igRestApiClient.getOTCOpenPositionsV1();                   
                    if (positionsResponse && (positionsResponse.Response != null) && (positionsResponse.Response.positions != null))                       
                    {
                        _positions.Clear();
                        if (positionsResponse.Response.positions.Count > 0)
                        {                            
                            AppendRestDataMessage(JsonConvert.SerializeObject(positionsResponse.Response, Formatting.Indented));
                            AppendActivityMessage(String.Format("{0} positions returned", positionsResponse.Response.positions.Count));
                            
                            foreach (var position in positionsResponse.Response.positions)
                            {
                                _positions.Add(position);
                            }
                            AppendActivityMessage("Positions found = " + _positions.Count);

                            cbPositions.Enabled = true;
                        }
                        else
                        {
                            AppendActivityMessage("You have no positions");
                        }
                    }
                    else
                    {
                        AppendActivityMessage("Response error");
                    }
                }
                catch (Exception ex)
                {
                    AppendActivityMessage(ex.Message);
                }
            }
        }

        private async void searchButton_Click(object sender, EventArgs e) 
        {
            try 
            {
                AppendActivityMessage("Searching markets for input string " + searchTextbox.Text);               
                var response = await _igRestApiClient.searchMarket(searchTextbox.Text);
                if (response && (response.Response != null) && (response.Response.markets.Count > 0))
                {
                    AppendRestDataMessage(JsonConvert.SerializeObject(response.Response, Formatting.Indented));
                }
                else
                {
                    AppendRestDataMessage("No instruments returned from Search");
                }
            } 
            catch (Exception ex) 
            {
                AppendActivityMessage(ex.Message);
            }
        }

        public void AppendActivityMessage(string message) 
        {
            if (activityTextbox.InvokeRequired) 
            {
                SetTextCallback d = AppendActivityMessage;
                Invoke(d, new object[] { message });
            } 
            else 
            {
                activityTextbox.AppendText(message + Environment.NewLine);
            }
        }

        public void AppendRestDataMessage(string message) {
            if (restDataTextbox.InvokeRequired) {
                SetTextCallback d = AppendRestDataMessage;
                Invoke(d, new object[] { message });
            } else {
                restDataTextbox.AppendText(message + Environment.NewLine);
            }
        }
        
        public void AppendStreamingDataMessage(string message) 
        {
            if (streamingDataTextbox.InvokeRequired) 
            {
                SetTextCallback d = AppendStreamingDataMessage;
                Invoke(d, new object[] { message });
            } 
            else 
            {
                streamingDataTextbox.AppendText(message + Environment.NewLine);
            }
        }

        private void DemoDotNetClientForm_FormClosed(object sender, FormClosedEventArgs e) {
            Application.Exit();
        }

        private async void workingOrdersButton_Click(object sender, EventArgs e) 
        {           
            AppendActivityMessage("Retrieving working orders");

            var response = await _igRestApiClient.workingOrdersV1();
            if (response && (response.Response != null) && (response.Response.workingOrders != null))
            {
                AppendActivityMessage(String.Format("{0} working orders returned", response.Response.workingOrders.Count));

                _orders.Clear();
                foreach (var wo in response.Response.workingOrders)
                {                      
                    _orders.Add(wo);
                }

                AppendRestDataMessage(JsonConvert.SerializeObject(response.Response, Formatting.Indented));

                cbOrders.Enabled = true;               
            }
            else
            {
                AppendActivityMessage(string.Format("workingOrdersV1 - Response error {0}", response.StatusCode));
            }                     
        }

        private async void watchlistsButton_Click(object sender, EventArgs e) 
        {
            if (_igRestApiClient != null)
            {                
                AppendActivityMessage("Retrieving watchlists");

                var response = await _igRestApiClient.listOfWatchlists();
                if (response && (response.Response != null) && (response.Response.watchlists != null))
                {
                    foreach (var wl in response.Response.watchlists)
                    {
                        _watchlists.Add(wl);
                        btnMarketData.Enabled = true;
                    }

                    AppendRestDataMessage(JsonConvert.SerializeObject(response.Response, Formatting.Indented));
                }
                else
                {
                    AppendActivityMessage("Response error : " + response.StatusCode);
                }                               
            }
        }

        private async void btnMarketData_Click(object sender, EventArgs e)
        {
            // This is hardcoded to always request market data for the first watchlist. You will need to change this to allow customers to select different watchlists.
            const int marketDataForWatchlist = 0; 

            if ((_watchlists.Count > 0) && ( _igRestApiClient != null))
            {
                AppendActivityMessage(String.Format("Retrieving watchlist markets for watchlist called {0}",
                                                      _watchlists[marketDataForWatchlist].name));
                var response = await _igRestApiClient.instrumentsForWatchlist(_watchlists[marketDataForWatchlist].id);
                if (response && (response.Response != null) && (response.Response.markets != null))
                {
                    AppendRestDataMessage(JsonConvert.SerializeObject(response.Response, Formatting.Indented));

                    AppendActivityMessage(String.Format("{0} markets found", response.Response.markets.Count));

                    foreach (var wl in response.Response.markets)
                    {                        
                        _watchlistMarkets.Add(wl);
                        cbWatchlistItems.Enabled = true;                      
                    }                    
                }
                else
                {
                    AppendActivityMessage("InstrumentsForWatchlist: Response error");
                }
            }
        }

        private void buttonExit_Click(object sender, EventArgs e)
        {
            _myForm.Dispose();
        }    

        private async void btnAccountDetails_Click(object sender, EventArgs e)
        {
            try
            {               
                AppendActivityMessage("Get Account Details");

                var response = await _igRestApiClient.accountBalance();                                           
                if (response && (response.Response != null) && (response.Response.accounts != null))
                {
                    var accounts = response.Response.accounts;
                    if (accounts.Count > 0)
                    {
                        foreach (var account in accounts)
                        {
                            if (!_accountIds.ContainsKey(account.accountId))
                            {
                                _accountIds.Add(account.accountId, account.accountType);
                                AppendRestDataMessage(String.Format("Account Id: {0} AccountType : {1}",
                                                                    account.accountId,
                                                                    account.accountType));
                            }
                        }
                        AppendRestDataMessage(JsonConvert.SerializeObject(response.Response, Formatting.Indented));
                    }
                }
                else
                {
                    AppendActivityMessage("HttpStatusCode error : statuscode = " + response.StatusCode);
                }
            }
            catch (Exception ex)
            {
                AppendActivityMessage(ex.Message);
            }
        }
       
        private void btnLogout_Click(object sender, EventArgs e)
        {
            try
            {
                AppendActivityMessage("Logging out ...");

                try
                {
                    _igRestApiClient.logout();
                }
                catch (Exception ex)
                {                    
                    AppendActivityMessage(ex.Message);
                }

                EnableCommandButtons(false);

                if (_igStreamApiClient != null)
                {
                    cbAccountSubscription.CheckState    = CheckState.Unchecked;
                    cbPositions.CheckState              = CheckState.Unchecked;
                    cbOrders.CheckState                 = CheckState.Unchecked;
                    cbAccountSubscription.CheckState    = CheckState.Unchecked;
                    cbTradeSubscription.CheckState      = CheckState.Unchecked;
                    cbWatchlistItems.CheckState         = CheckState.Unchecked;

                    if (_tradeSubscriptionStk != null)
                    {
                        _igStreamApiClient.UnsubscribeTableKey(_tradeSubscriptionStk);
                        _tradeSubscriptionStk = null;
                    }
                    if (_positionSubscriptionStk != null)
                    {
                        _igStreamApiClient.UnsubscribeTableKey(_positionSubscriptionStk);
                        _positionSubscriptionStk = null;
                    }
                    if (_orderSubscriptionStk != null)
                    {
                        _igStreamApiClient.UnsubscribeTableKey(_orderSubscriptionStk);
                        _orderSubscriptionStk = null;
                    }
                    if (_watchlistItemSubscriptionStk != null)
                    {
                        _igStreamApiClient.UnsubscribeTableKey(_watchlistItemSubscriptionStk);
                        _watchlistItemSubscriptionStk = null;
                    }
                    if (_accountsStk != null)
                    {
                        _igStreamApiClient.UnsubscribeTableKey(_accountsStk);
                        _accountsStk = null;
                    }
                    _igStreamApiClient.disconnect();
                }

                cbPositions.Enabled = false;
                cbOrders.Enabled = false;
                cbWatchlistItems.Enabled = false;
                cbTradeSubscription.Enabled = false;
                cbAccountSubscription.Enabled = false;              

                AppendActivityMessage("Logged out");
            }
            catch (Exception ex)
            {
                AppendActivityMessage(ex.Message);
            }
        }              

        //
        // Streaming Data  ON/OFF...
        //
        private void cbPositions_CheckedChanged(object sender, EventArgs e)
        {
            if (cbPositions.CheckState == CheckState.Checked)
            {
                // Subscribe to positions...
                if ((_positions != null) && (_positions.Count > 0))
                {
                    // Now subscribe to live prices for these positions...
                    _positionSubscription = new PositionSubscription(this);

	                string[] uc =
		                (from position in _positions where position.market.streamingPricesAvailable == true select position.market.epic)
			                .Distinct().ToArray();

	                if (uc.Length > 0)
	                {
		                _positionSubscriptionStk = _igStreamApiClient.subscribeToMarketDetails( uc, _positionSubscription);
	                }
	                else
	                {
						AppendActivityMessage("Streaming prices not enabled for these positions"); 
	                }
                }
                else
                {
                    AppendActivityMessage("There are no Positions to subscribe too. Either server is not responding or there are no positions.");
                }
            }
            else
            {
                // Unsubscribe from positions...
                if ((_positionSubscriptionStk != null) && (_igStreamApiClient != null))
                {
                    _igStreamApiClient.UnsubscribeTableKey(_positionSubscriptionStk);
                }
            }
        }

        private void cbOrders_CheckedChanged(object sender, EventArgs e)
        {
            try
            {
                if (cbOrders.CheckState == CheckState.Checked)
                {
	                if ((_orders != null) && (_orders.Count > 0))
	                {
		                _orderSubscription = new OrderSubscription(this);

		                string[] ue =
			                (from order in _orders
				                where order.marketData.streamingPricesAvailable
				                select order.marketData.epic)
				                .Distinct().ToArray();

		                if (ue.Length > 0)
		                {
			                _orderSubscriptionStk = _igStreamApiClient.subscribeToMarketDetails(
					                (from order in _orders where order.marketData.streamingPricesAvailable select order.marketData.epic)
						                .Distinct().ToArray(), _orderSubscription);
		                }
		                else
		                {
			                AppendActivityMessage("These orders do not have streaming prices enabled");
		                }
	                }
					else
					{
						AppendActivityMessage("there are no Orders to subscribe too. Either server is not responding or there are no orders.");
					}

                }
                else
                {
                    // Unsubscribe from positions...
                    if ((_orderSubscriptionStk != null) && (_igStreamApiClient != null))
                    {
                        _igStreamApiClient.UnsubscribeTableKey(_orderSubscriptionStk);
                    }
                }
            }
            catch (Exception ex)
            {
                AppendActivityMessage("Warning - problems subscribing to orders : " + ex.Message);
            }
        }

        private void cbWatchlistItems_CheckedChanged(object sender, EventArgs e)
        {
            if (cbWatchlistItems.CheckState == CheckState.Checked)
            {
                // Subscribe to positions...
                if ((_watchlistMarkets != null) && (_watchlistMarkets.Count != 0 ))
                {
                    _watchlistItemSubscription = new WatchlistMarketSubscription(this);

					string[] ue =
						  (from watchlistMarket in _watchlistMarkets
						   where watchlistMarket.streamingPricesAvailable
						   select watchlistMarket.epic)
							  .Distinct().ToArray();

	                if (ue.Length > 0)
	                {
		                _watchlistItemSubscriptionStk =
			                _igStreamApiClient.subscribeToMarketDetails(ue, _watchlistItemSubscription);
	                }
	                else
	                {
						AppendActivityMessage("there are no streaming prices for these instruments");
	                }
                }
                else
                {
                    AppendActivityMessage("there are no Watchlist Items to subscribe too. Either server is not responding or there are no watchlist items");
                }
            }
            else
            {
                // Unsubscribe from watchlist items
                if ((_igStreamApiClient != null) && (_watchlistItemSubscriptionStk != null))
                {
                    _igStreamApiClient.UnsubscribeTableKey(_watchlistItemSubscriptionStk);
                }
            }
        }
      
        private void cbAccountSubscription_CheckedChanged(object sender, EventArgs e)
        {
            if (cbAccountSubscription.CheckState == CheckState.Checked)
            {
                try
                {
                    if (_currentAccount != null)
                    {
                        _accountBalanceSubscription = new AccountBalanceSubscription(this);
                        if (_igStreamApiClient != null)
                        {
                            _accountsStk = _igStreamApiClient.subscribeToAccountDetailsKey(_currentAccount,
                                                                                           _accountBalanceSubscription);
                            AppendActivityMessage("AccountDetails : Successfully Subscribed");
                        }
                    }
                }
                catch (Exception ex)
                {
                    AppendStreamingDataMessage(ex.Message);
                }
            }
            else
            {
                if ((_accountsStk != null) && ( _igStreamApiClient != null))
                {
                    _igStreamApiClient.UnsubscribeTableKey(_accountsStk);
                    AppendActivityMessage("AccountDetails : Unsubscribe");
                }
            }
        }

        private void cbTradeSubscription_CheckedChanged(object sender, EventArgs e)
        {
            try
            {
                if (_igStreamApiClient != null)
                {
                    if (cbTradeSubscription.CheckState == CheckState.Checked)
                    {
                        _tradeConfirmsSubscription = new TradeConfirmsSubscription(this);
                        _tradeSubscriptionStk = _igStreamApiClient.subscribeToTradeSubscription(_currentAccount,
                                                                                                _tradeConfirmsSubscription);
                        AppendActivityMessage("TradeSubscription : Subscribe");
                    }
                    else
                    {
                        if (_tradeSubscriptionStk != null)
                        {
                            _igStreamApiClient.UnsubscribeTableKey(_tradeSubscriptionStk);
                            AppendActivityMessage("TradeSubscription : Unsubscribe");
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                AppendStreamingDataMessage(ex.Message);
            }
        }                
    }

    // L1 Subscriptions...

    public class AccountBalanceSubscription : HandyTableListenerAdapter 
    {        
        private readonly DemoDotNetClientForm _form;

        public AccountBalanceSubscription(DemoDotNetClientForm form) 
        {
           _form = form;            
        }

        public override void OnUpdate(int itemPos, string itemName, IUpdateInfo update)
        {                             
            _form.AppendStreamingDataMessage("account balance update - P/L : " + update.GetNewValue("PNL"));
            _form.AppendStreamingDataMessage("account balance update - DEPOSIT : " + update.GetNewValue("DEPOSIT"));
            _form.AppendStreamingDataMessage("account balance update - AVAILABLE_CASH : " + update.GetNewValue("AVAILABLE_CASH"));
        }
    }

    public class TradeConfirmsSubscription : HandyTableListenerAdapter 
    {
        private readonly DemoDotNetClientForm _form;
       
        public TradeConfirmsSubscription(DemoDotNetClientForm form) 
        {          
           _form = form;
        }

        public override void OnUpdate(int itemPos, string itemName, IUpdateInfo update) 
        {     
            _form.AppendStreamingDataMessage("trade confirm: " + update.GetNewValue("CONFIRMS"));
            _form.AppendStreamingDataMessage("open position updates: " + update.GetNewValue("OPU"));
            _form.AppendStreamingDataMessage("working order updates: " + update.GetNewValue("WOU"));   
        }
    }

    public class WatchlistMarketSubscription : HandyTableListenerAdapter
    {
        private readonly DemoDotNetClientForm _form;

        // <Summary>
        // Get L1 Prices for Watchlist instruments...
        // </Summary>
        public string GetL1Prices(int itemPos, string itemName, IUpdateInfo update)
        {
            return L1PriceUpdates(itemPos, itemName, update);
        }

        // <Summary>
        // Subscribe to L1 Prices for watchlist instruments..
        // </Summary>
        public WatchlistMarketSubscription(DemoDotNetClientForm form)
        {
            _form = form;
        }

        // <Summary>
        // Lighstreamer callback - Watchlist instrument price updates...
        // </Summary>
        public override void OnUpdate(int itemPos, string itemName, IUpdateInfo update)
        {
            try
            {
                _form.AppendStreamingDataMessage(GetL1Prices(itemPos, itemName, update));
            }
            catch (Exception ex)
            {
                _form.AppendStreamingDataMessage(ex.Message);
            }    
        }
    }

    public class PositionSubscription : HandyTableListenerAdapter
    {
        private readonly DemoDotNetClientForm _form;
       
        public string GetL1Prices(int itemPos, string itemName, IUpdateInfo update)
        {                      
            return L1PriceUpdates(itemPos, itemName, update);
        }

        public L1LsPriceData GetL1PricesData(int itemPos, string itemName, IUpdateInfo update)
        {           
            return L1LsPriceUpdateData(itemPos, itemName, update);
        }

        // <Summary>
        // Subscribe to L1 Prices for Positions...
        // </Summary>
        public PositionSubscription(DemoDotNetClientForm form)
        {
            _form = form;
        }

        // <Summary>
        // Lighstreamer callback - Position price updates...
        // </Summary>
        public override void OnUpdate(int itemPos, string itemName, IUpdateInfo update)
        {
            try
            {
                _form.AppendStreamingDataMessage(GetL1Prices(itemPos, itemName, update));
            }
            catch (Exception ex)
            {
                _form.AppendStreamingDataMessage(ex.Message);
            }
        }
    }

    public class OrderSubscription : HandyTableListenerAdapter
    {
        private readonly DemoDotNetClientForm _form;
       
        public string GetL1Prices(int itemPos, string itemName, IUpdateInfo update)
        {                       
            return L1PriceUpdates(itemPos, itemName, update);
        }

        // <Summary>
        // Subscribe to L1 Prices for Orders...
        // </Summary>
        public OrderSubscription(DemoDotNetClientForm form)
        {
            _form = form;
        }

        // <Summary>
        // Lighstreamer callback - Order price updates...
        // </Summary>
        public override void OnUpdate(int itemPos, string itemName, IUpdateInfo update)
        {
            try
            {
                _form.AppendStreamingDataMessage(GetL1Prices(itemPos, itemName, update));
            }
            catch (Exception ex)
            {
                _form.AppendStreamingDataMessage(ex.Message);
            }
        }
    }     

}
