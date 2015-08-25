using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Lightstreamer.DotNet.Client;

namespace MidaxLib
{
    public class ReplayStreamingClient : IAbstractStreamingClient
    {
        void IAbstractStreamingClient.Connect(string username, string password, string apiKey)
        {
        }

        void IAbstractStreamingClient.Subscribe(string[] epics, IHandyTableListener tableListener)
        {
        }

        void IAbstractStreamingClient.Unsubscribe()
        {
        }
    }

    public class ReplayConnection : MarketDataConnection
    {
        public override void Connect()
        {
        }
    }
}
