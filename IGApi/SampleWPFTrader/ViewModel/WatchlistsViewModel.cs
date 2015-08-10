using System;
using System.Collections.ObjectModel;
using System.Linq;
using IGPublicPcl;
using Lightstreamer.DotNet.Client;
using SampleWPFTrader.Model;

namespace SampleWPFTrader.ViewModel
{
    /// <Summary>
    ///
    /// IG Trader WPF Sample Application 
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
    public class WatchlistsViewModel : ViewModelBase
    {
        private ObservableCollection<IgPublicApiData.WatchlistModel> _watchlists;
        private ObservableCollection<IgPublicApiData.WatchlistMarketModel> _watchlistMarkets;

        private IgPublicApiData.WatchlistModel _currentWatchlist;

        //
        // This value is used to indicate the index of the watchlist that we have subscribed to, in the 
        // watchlists array...
        //

        private int _watchlistIndex = 0;
        private int _watchlistMarketIndex = 0;

        private SubscribedTableKey _watchlistL1PricesSubscribedTableKey;

        private L1PricesSubscription _l1PricesSubscription;            
     
        public WatchlistsViewModel()
        {           
            _watchlists = new ObservableCollection<IgPublicApiData.WatchlistModel>();
            _watchlistMarkets = new ObservableCollection<IgPublicApiData.WatchlistMarketModel>();

            // Initialise to first index in datagrid.
            WatchlistIndex = 0;
            WatchlistMarketIndex = 0;

            WireCommands();
            
            _l1PricesSubscription = new L1PricesSubscription(this);                                  

            _watchlistL1PricesSubscribedTableKey = new SubscribedTableKey();
            _watchlistL1PricesSubscribedTableKey = null;
        }

        public ObservableCollection<IgPublicApiData.WatchlistModel> Watchlists
        {
            get { return _watchlists; }
            set { _watchlists = value; }
        }

        public ObservableCollection<IgPublicApiData.WatchlistMarketModel> WatchlistMarkets
        {
            get { return _watchlistMarkets; }
            set { _watchlistMarkets = value; }
        }

        private void WireCommands()
        {            
            GetWatchlistsCommand = new RelayCommand(GetRestWatchlists);
            GetWatchlistMarketsCommand = new RelayCommand(GetRestWatchlistMarkets);           
            GetWatchlistsCommand.IsEnabled = true;           
        }

        public RelayCommand UpdateWatchlistCommand
        {
            get;
            private set;
        }      

        public RelayCommand GetWatchlistsCommand
        {
            get;
            private set;
        }

        public RelayCommand GetWatchlistMarketsCommand
        {
            get;
            private set;
        }
          
        public IgPublicApiData.WatchlistModel CurrentWatchlist
        {
            get
            {
                return _currentWatchlist;
            }

            set
            {
                if (_currentWatchlist != value)
                {
                    _currentWatchlist = value;
                    RaisePropertyChanged("CurrentWatchlist");
                    UpdateWatchlistCommand.IsEnabled = true;
                }
            }
        }

        public int WatchlistIndex
        {
            get
            {                
                return _watchlistIndex;
            }
            set
            {
                if (_watchlistIndex != value)
                {                    
                    _watchlistIndex = value;
                    RaisePropertyChanged("WatchlistIndex");
                    WatchlistIndexChanged();

                    GetWatchlistMarketsCommand.IsEnabled = true;
                }
            }
        }
       
        public void WatchlistIndexChanged()
        {
	        if (WatchlistIndex >= 0)
	        {
		        ClearWatchlistMarkets();

		        UpdateWatchlistsMessage("Watchlist Selected = " + Watchlists[WatchlistIndex].WatchlistName);

		        // First UnSubscribe from instruments in old watchlist ...            
		        UnsubscribeFromWatchlistInstruments();

		        // Now subscribe to instruments in new watchlist ( indicated by WatchlistIndex ).
		        GetRestWatchlistMarkets();

		        UpdateWatchlistsMessage("Subscribing to L1 instrument prices for watchlist = " +
		                                Watchlists[WatchlistIndex].WatchlistName);
	        }
        }


        private void UnsubscribeFromWatchlistInstruments()
        {            
            if ((igStreamApiClient != null) && (_watchlistL1PricesSubscribedTableKey != null) && (LoggedIn))
            {
                igStreamApiClient.UnsubscribeTableKey(_watchlistL1PricesSubscribedTableKey);
                _watchlistL1PricesSubscribedTableKey = null;

                UpdateWatchlistsMessage("WatchlistsViewModel : Unsubscribing from L1 Prices for Watchlists");
            }
        }
      
        public void WatchlistTabChanged()
        {
            if (WatchlistTabSelected)
            {
                UpdateWatchlistsMessage("Watchlist Tab selected");
                if (LoggedIn)
                {
                    GetRestWatchlists();
                }
                else
                {
                    UpdateWatchlistsMessage("Please log in first");
                }
            }
            else
            {
                UpdateWatchlistsMessage("Watchlist Tab de-selected");
                UnsubscribeFromWatchlistInstruments();
            }
        }

        private bool _watchlistTabSelected;
        public bool WatchlistTabSelected
        {
            get
            {
                return _watchlistTabSelected;
            }
            set
            {
                if (_watchlistTabSelected != value)
                {
                    _watchlistTabSelected = value;
                    WatchlistTabChanged();
                    RaisePropertyChanged("WatchlistTabSelected");
                }
            }
        }


        public int WatchlistMarketIndex
        {
            get
            {
                return _watchlistMarketIndex;
            }

            set
            {
                if (_watchlistMarketIndex != value)
                {
                    _watchlistMarketIndex = value;                   
                    RaisePropertyChanged("WatchlistMarketIndex");                   
                }
            }
        }

        private string _watchlistsData;
        public string WatchlistsData
        {
            get { return _watchlistsData; }
            set
            {
                if ((_watchlistsData != value) && (value != null))
                {
                    _watchlistsData = value;
                    RaisePropertyChanged("WatchlistsData");
                }
            }
        }
       
        private string _watchlistL1PriceUpdates;
        public string WatchlistL1PriceUpdates
        {
            get { return _watchlistL1PriceUpdates; }
            set
            {
                if ((_watchlistL1PriceUpdates != value) && (value != null))
                {
                    _watchlistL1PriceUpdates = value;
                    RaisePropertyChanged("WatchlistL1PriceUpdates");
                }
            }
        }
      
        public void UpdateWatchlistsMessage(string message)
        {
            WatchlistsData += message + Environment.NewLine;
        }

        public void UpdateWatchlistL1PricesMessage(string message)
        {
            WatchlistL1PriceUpdates += message + Environment.NewLine;
        }
      
        private void ClearWatchlistMarkets()
        {
            if (WatchlistMarkets != null)
            {
                UnsubscribeFromWatchlistInstruments();
                WatchlistMarkets.Clear();                                   
            }
        }
       
        // <Summary>
        // GetRestWatchlists
        // </Summary>
        public async void GetRestWatchlists()
        {
            try
            {               
                UpdateWatchlistsMessage("Retrieving watchlists");
                var response = await igRestApiClient.listOfWatchlists();
                if (response  && (response.Response.watchlists != null))
                {
                    Watchlists.Clear();
                    foreach (var wl in response.Response.watchlists)
                    {
                        var wm = new IgPublicApiData.WatchlistModel();
                        wm.WatchlistId = wl.id;
                        wm.WatchlistName = wl.name;
                        wm.Editable = wl.editable;
                        wm.Deletable = wl.deleteable;  
                      
                        Watchlists.Add(wm);                       
                    }

                    if (Watchlists.Count >= 1)
                    {
                        GetWatchlistMarketsCommand.IsEnabled = true;
                        WatchlistIndex = 0;                       
                    }

                    // Let's get the instruments for the 1st watchlist in the list.

                    WatchlistIndex = 0;
                    GetRestWatchlistMarkets();
                }
                else
                {
                    UpdateWatchlistsMessage("GetRestWatchlists : no watchlists returned from server");
                }

            }
            catch (Exception ex)
            {
                UpdateWatchlistsMessage(ex.Message);
            }
        }
    
        // <Summary>
        // GetRestWatchlistMarkets
        // </Summary>
        public async void GetRestWatchlistMarkets()
        {           
            try
            {               
                if ((Watchlists != null ) && ( WatchlistIndex < Watchlists.Count))
                {                    
                    UpdateWatchlistsMessage(String.Format("Retrieving watchlist markets for watchlist called {0}",
                                                          Watchlists[WatchlistIndex].WatchlistName));
                    var response = await igRestApiClient.instrumentsForWatchlist(Watchlists[WatchlistIndex].WatchlistId);
                    if (response != null) 
                    {
                        if (response && (response.Response.markets != null) && (response.Response.markets.Count > 0))
                        {
                            WatchlistMarkets.Clear();
                            foreach (var wl in response.Response.markets)
                            {
                                var wim = new IgPublicApiData.WatchlistMarketModel();
                                wim.Model = new IgPublicApiData.InstrumentModel();
                                wim.Model.High = wl.high;
                                wim.Model.Low = wl.low;
                                wim.Model.Epic = wl.epic;
                                wim.Model.Bid = wl.bid;
                                wim.Model.Offer = wl.offer;
                                wim.Model.PctChange = wl.percentageChange;
                                wim.Model.NetChange = wl.netChange;
                                wim.Model.InstrumentName = wl.instrumentName;
                                wim.Model.StreamingPricesAvailable = wl.streamingPricesAvailable;
                                wim.Model.MarketStatus = wl.marketStatus;
                                WatchlistMarkets.Add(wim);
                            }

                            // Get unique epics for these orders ( we don't want to subscribe to the same epic twice )
                            var uniqueEpics = (from dbo in WatchlistMarkets
                                               where dbo.Model.StreamingPricesAvailable == true
                                               select dbo.Model.Epic).Distinct().ToArray();

                            if (uniqueEpics.Length != 0)
                            {
                                SubscribeL1WatchlistPrices(uniqueEpics);
                            }
                            else
                            {
								UpdateWatchlistsMessage("There are no watchlist instruments with streaming prices enabled");
                            }
                        }
                        else
                        {
                            UpdateWatchlistsMessage("HttpStatusCode error: " + response.StatusCode);
                        }
                    }
                    else
                    {
                        UpdateWatchlistsMessage("GetRestWatchlistMarkets : null response from server");
                    }
                }
            }
            catch (Exception ex)
            {
                UpdateWatchlistsMessage(ex.Message);
            }
        }       

        // <Summary>
        // Lightstreamer Subscribe to L1 Prices...
        // </Summary>
        private void SubscribeL1WatchlistPrices(string[] watchlistItems)
        {                                   
            try
            {                                     
                if (igStreamApiClient != null) 
                {                       
                    _watchlistL1PricesSubscribedTableKey = igStreamApiClient.subscribeToMarketDetails(watchlistItems, _l1PricesSubscription);
                    UpdateWatchlistsMessage("Successfully subscribed to Watchlist : " + Watchlists[WatchlistIndex].WatchlistName);                        
                }                                   
            }
            catch (Exception ex)
            {
                UpdateWatchlistsMessage("Exception when trying to subscribe to Watchlist l1 prices: " + ex.Message);               
            }            
        }
      
        // <Summary>
        // The Watchlist L1 Price Subscription
        // </Summary>
        public class L1PricesSubscription : HandyTableListenerAdapter
        {
            private readonly WatchlistsViewModel _watchlistsViewModel;            
        
            public L1PricesSubscription(WatchlistsViewModel wvm)
            {
                _watchlistsViewModel = wvm;
            }
           
            public override void OnUpdate(int itemPos, string itemName, IUpdateInfo update)
            {
                try
                {
                    _watchlistsViewModel.WatchlistL1PriceUpdates = L1PriceUpdates(itemPos, itemName, update);

                    var wlmUpdate = L1LsPriceUpdateData(itemPos, itemName, update);

                    var epic = itemName.Replace("L1:", "");

                    foreach (var watchlistItem in _watchlistsViewModel.WatchlistMarkets)
                    {
                        if (watchlistItem.Model.Epic == epic)
                        {
                            watchlistItem.Model.Epic = epic;
                            watchlistItem.Model.Bid = wlmUpdate.Bid;
                            watchlistItem.Model.Offer = wlmUpdate.Offer;
                            watchlistItem.Model.NetChange = wlmUpdate.Change;
                            watchlistItem.Model.PctChange = wlmUpdate.ChangePct;
                            watchlistItem.Model.Low = wlmUpdate.Low;
                            watchlistItem.Model.High = wlmUpdate.High;
                            watchlistItem.Model.Open = wlmUpdate.MidOpen;
                            watchlistItem.UpdateTime = wlmUpdate.UpdateTime;
                            watchlistItem.Model.MarketStatus = wlmUpdate.MarketState;
                        }
                    }
                }
                catch (Exception ex)
                {
                    _watchlistsViewModel.WatchlistL1PriceUpdates = ex.Message;
                }                                                      
            }                               
        }
    }
}
