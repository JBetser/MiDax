using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MidaxLib;

namespace Calibrator
{
    class NeuralNetworkWMA_4_2 : NeuralNetworkForCalibration
    {
        public NeuralNetworkWMA_4_2(string stockid,
            Dictionary<string, List<CqlQuote>> marketData, 
            Dictionary<string, List<CqlQuote>> indicatorData,
            Dictionary<string, List<double>> profitData)
            : base("WMA_4_2", stockid, 4, 1, new List<int>() { 2 })
        {
            // build the neural network and the training set
            var daxQuotes = new List<double>();
            foreach (var quote in marketData[StockId])
                daxQuotes.Add((double)quote.MidPrice());
            var wma2 = new List<double>();
            foreach (var quote in indicatorData["WMA_2_" + StockId])
                wma2.Add((double)quote.MidPrice());
            var wma10 = new List<double>();
            foreach (var quote in indicatorData["WMA_10_" + StockId])
                wma10.Add((double)quote.MidPrice());
            var wma60 = new List<double>();
            foreach (var quote in indicatorData["WMA_60_" + StockId])
                wma60.Add((double)quote.MidPrice());            
            for (int idxQuote = 0; idxQuote < daxQuotes.Count; idxQuote++)
            {
                var newInputset = new List<double>();
                newInputset.Add(daxQuotes[idxQuote]);
                newInputset.Add(wma2[idxQuote]);
                newInputset.Add(wma10[idxQuote]);
                newInputset.Add(wma60[idxQuote]);
                _annInputs.Add(newInputset);
                var newOutputset = new List<double>();
                newOutputset.Add(profitData[StockId][idxQuote]);
                _annOutputs.Add(newOutputset);
            }            
        }          
    }
}
