using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MidaxLib;

namespace Calibrator
{
    class NeuralNetwork7N : NeuralNetwork
    {
        List<List<double>> _annInputs = new List<List<double>>();
        List<List<double>> _annOutputs = new List<List<double>>();

        public NeuralNetwork7N(Dictionary<string, List<CqlQuote>> marketData, 
            Dictionary<string, List<CqlQuote>> indicatorData,
            Dictionary<string, List<double>> profitData)
            : base(4, 1, new List<int>() { 2 })
        {
            // build the neural network and the training set
            var daxQuotes = new List<double>();
            foreach (var quote in marketData["IX.D.DAX.DAILY.IP"])
                daxQuotes.Add((double)quote.MidPrice());
            var wma2 = new List<double>();
            foreach (var quote in indicatorData["WMA_2_IX.D.DAX.DAILY.IP"])
                wma2.Add((double)quote.MidPrice());
            var wma10 = new List<double>();
            foreach (var quote in indicatorData["WMA_10_IX.D.DAX.DAILY.IP"])
                wma10.Add((double)quote.MidPrice());
            var wma60 = new List<double>();
            foreach (var quote in indicatorData["WMA_60_IX.D.DAX.DAILY.IP"])
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
                newOutputset.Add(profitData["IX.D.DAX.DAILY.IP"][idxQuote]);
                _annOutputs.Add(newOutputset);
            }            
        }

        public void Train(double max_error)
        {
            base.Train(_annInputs, _annOutputs, (double)_annInputs.Count * 0.01, max_error);
        }            
    }
}
