using System;
using System.Collections.ObjectModel;
using System.Linq;
using IGPublicPcl;
using Lightstreamer.DotNet.Client;
using SampleWPFTrader.Model;
using dto.endpoint.browse;

namespace SampleWPFTrader.ViewModel
{
    /// <Summary>
    ///
    /// IG Trader WPF Sample Application 
    /// 
    /// BrowseViewModel : This file contains all the business logic to handle the Browse command ( i.e. browsing nodes, 
    /// requesting sub-nodes and obtaining the markets for the node, and subscribing to Lightstreamer prices for these instruments ).
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
    public class BrowseViewModel : ViewModelBase
    {       
        private ObservableCollection<HierarchyNode> _browseNodes;      
        private ObservableCollection<IgPublicApiData.BrowseModel> _browseMarkets;

        // LS subscriptions...
        private L1BrowsePricesSubscription _l1BrowsePricesSubscription;
        private SubscribedTableKey _browseSubscriptionTableKey;

        private bool _browseTabSelected;
        public bool BrowseTabSelected
        {
            get
            {
                return _browseTabSelected;
            }
            set
            {
                if (_browseTabSelected != value)
                {
                    _browseTabSelected = value;
                    BrowseTabChanged();
                    RaisePropertyChanged("BrowseTabSelected");
                }
            }
        }

        public void BrowseTabChanged()
        {
            if (BrowseTabSelected)
            {
                UpdateMessage("Browse Tab selected");
                // Get Rest Orders and then subscribe
                if (LoggedIn)
                {
                    GetBrowseMarketsRoot();
                    UpdateMessage("Get browse root.");
                }
                else
                {
                    UpdateMessage("Please log in first");
                }
            }
            else
            {
                UpdateMessage("Browse Tab de-selected");                
                UnsubscribeFromBrowsePrices();
            }
        }

        public BrowseViewModel()
        {
            InitialiseViewModel();
             
             _browseNodes = new ObservableCollection<HierarchyNode>();            
             _browseMarkets = new ObservableCollection<IgPublicApiData.BrowseModel>();         
                       
            NodeIndex = 0;

            // Initialise LS subscriptions            
            _l1BrowsePricesSubscription = new L1BrowsePricesSubscription(this);

            // initialise the LS SubscriptionTableKeys          
            _browseSubscriptionTableKey = new SubscribedTableKey();
            _browseSubscriptionTableKey = null;

            WireCommands();
        }

        private void WireCommands()
        {           
            GetBrowseMarketsCommand = new RelayCommand(GetBrowseMarkets);            
            GetBrowseRootCommand = new RelayCommand(GetBrowseMarketsRoot);            
            GetBrowseMarketsCommand.IsEnabled = false;
            GetBrowseRootCommand.IsEnabled = true;           
        }
            
        private HierarchyNode _selectedItem;
        public HierarchyNode SelectedItem
        {
            get { return _selectedItem; }
            set
            {
                if (_selectedItem != value)
                {
                    _selectedItem = value;
                    RaisePropertyChanged("SelectedItem");
                }
            }
        }

        public RelayCommand GetBrowseRootCommand
        {
            get;
            private set;
        }
           
        public RelayCommand GetBrowseMarketsCommand
        {
            get;
            private set;
        }
             
        public ObservableCollection<HierarchyNode> BrowseNodes
        {
            get { return _browseNodes; }
            set { _browseNodes = value; }
        }
      
        public ObservableCollection<IgPublicApiData.BrowseModel> BrowseMarkets
        {
            get { return _browseMarkets; }
            set { _browseMarkets = value; }
        }
      
        private int _nodeIndex;
        public int NodeIndex
        {
            get { return _nodeIndex; }
            set
            {
                if (_nodeIndex != value)
                {
                    _nodeIndex = value;
                    RaisePropertyChanged("NodeIndex");
                }
            }
        }
    
        private string _browseData;
        public string BrowseData
        {
            get { return _browseData; }
            set
            {
                if ((_browseData != value) && (value != null))
                {
                    _browseData = value;
                    RaisePropertyChanged("BrowseData");
                }
            }
        }       

        public void UpdateMessage(string message)
        {
            BrowseData += message + Environment.NewLine;
        }

        public void SubscribeToBrowsePrices(string[] epics)
        {            
            try
            {
                // Subscribe to L1 price updates for the instruments contained in this browse node...
                _browseSubscriptionTableKey = igStreamApiClient.subscribeToMarketDetails(epics, _l1BrowsePricesSubscription);
                UpdateMessage("Subscribed Successfully to instruments contained within this browse node.");             
            }
            catch (Exception ex)
            {
                UpdateMessage("Could not subscribe to browse instruments : " + ex.Message);
            }                   
        }       

        public void UnsubscribeFromBrowsePrices()
        {
            if ((igStreamApiClient != null) && (_browseSubscriptionTableKey != null) && (LoggedIn))
            {
                igStreamApiClient.UnsubscribeTableKey(_browseSubscriptionTableKey);
                _browseSubscriptionTableKey = null;
                UpdateMessage("Unsubscribed from Browse Node Prices");
            }
        }

        public async void GetBrowseMarkets()
        {            
            try
            {
                if (NodeIndex < 0)
                {
                    UpdateMessage("Please select an item");
                    return;
                }

                if ((igRestApiClient != null) && (BrowseNodes != null))
                {                    
                    if (NodeIndex >= BrowseNodes.Count)
                    {
                        UpdateMessage("Please select a node first");
                        return;
                    }

                    UnsubscribeFromBrowsePrices();

                    var response = await igRestApiClient.browse(BrowseNodes[NodeIndex].id);
                   
                    if (response && (response.Response != null))
                    {                                             
                        if (response.Response.nodes != null)
                        {
                            BrowseNodes.Clear();

                            UpdateMessage("Retrieving browse market nodes / markets ");

                            foreach (var node in response.Response.nodes)
                            {
                                if (node != null)
                                {                                                                       
                                    BrowseNodes.Add(node);
                                    UpdateMessage("Browse Node found: " + node.name);
                                }
                            }
                        }
                        else
                        {
                            if (response.Response.markets != null)
                            {
                                BrowseMarkets.Clear();

                                foreach (var market in response.Response.markets)
                                {
                                    if (market != null)
                                    {
                                        var bm = new IgPublicApiData.BrowseModel();

                                        bm.Model = new IgPublicApiData.InstrumentModel();
                                        bm.Model.Bid = market.bid;
                                        bm.Model.Offer = market.offer;
                                        bm.Model.High = market.high;
                                        bm.Model.Low = market.low;
                                        bm.Model.InstrumentName = market.instrumentName;
                                        bm.Model.Epic = market.epic;                           
                                        bm.Model.NetChange = market.netChange;
                                        bm.Model.PctChange = market.percentageChange;
                                        bm.Model.StreamingPricesAvailable = market.streamingPricesAvailable;

                                        BrowseMarkets.Add(bm);
                                        UpdateMessage(String.Format("Browse Market found: {0} epic:{1}",
                                                                    market.instrumentName, market.epic));
                                    }                                
                                }

                                //
                                // Subscribe to Browse Market Instrument Prices ( which are unique and have streaming prices enabled )
                                //
                                var epics = (from dbo in BrowseMarkets
                                where dbo.Model.StreamingPricesAvailable == true
                                select dbo.Model.Epic).Distinct().ToArray();

                                if (epics.Length != 0)
                                {
                                    SubscribeToBrowsePrices(epics);
                                }
                            }
                        }
                        
                    }
                    else
                    {
                        UpdateMessage("BrowseMarketNodex: no sub-nodes / markets for this node");
                    }
                }
            }
            catch (Exception ex)
            {
                UpdateMessage(ex.Message);
            }
        }
      
        public async void GetBrowseMarketsRoot()
        {
            try
            {
                if (LoggedIn)
                {
                    // Unsubscribe from any instruments we are currently subscribed to...
                    UnsubscribeFromBrowsePrices();
                   
                    var response = await igRestApiClient.browseRoot();

                    if (response && (response.Response != null) && (response.Response.nodes != null))
                    {
                        BrowseNodes.Clear();
                        BrowseMarkets.Clear();

                        foreach (var node in response.Response.nodes)
                        {
                            BrowseNodes.Add(node);
                        }

                        if (response.Response.nodes.Count > 0)
                        {
                            NodeIndex = 0;
                            GetBrowseMarketsCommand.IsEnabled = true;
                        }

                        UpdateMessage(String.Format("Browse Market data received for {0} nodes", response.Response.nodes.Count));
                    }
                    else
                    {
                        UpdateMessage("BrowseMarkets : no browse root nodes");
                    }
                }
                else
                {
                    UpdateMessage("Please log in first");
                }
            }
            catch (Exception ex)
            {
                UpdateMessage(ex.Message);
            }
        }

        public class L1BrowsePricesSubscription : HandyTableListenerAdapter
        {
            private readonly BrowseViewModel _browseViewModel;

            public L1BrowsePricesSubscription(BrowseViewModel bvm)
            {
                _browseViewModel = bvm;
            }

            public override void OnUpdate(int itemPos, string itemName, IUpdateInfo update)
            {
                try
                {
                    var browseUpdate = L1LsPriceUpdateData(itemPos, itemName, update);

                    var epic = itemName.Replace("L1:", "");
                   
                    foreach (var market in _browseViewModel.BrowseMarkets)                    
                    {
                        if (market.Model.Epic == epic)
                        {                                            
                            market.Model.LsItemName = itemName;
                            market.Model.Bid = browseUpdate.Bid;
                            market.Model.Offer = browseUpdate.Offer;
                            market.Model.NetChange = browseUpdate.Change;
                            market.Model.PctChange = browseUpdate.ChangePct;
                            market.Model.Low = browseUpdate.Low;
                            market.Model.High = browseUpdate.High;
                            market.Model.Open = browseUpdate.MidOpen;
                            market.Model.MarketStatus = browseUpdate.MarketState;
                        }
                    }

                }
                catch (Exception ex)
                {
                    //Display in text box..
                    _browseViewModel.BrowseData = ex.Message;
                }
            }
        }

    }

}
