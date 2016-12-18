using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MidaxLib;

namespace Calibrator
{
    class NeuralNetworkFX : NeuralNetworkForCalibration
    {
        public NeuralNetworkFX(string stockid,
            Dictionary<string, List<CqlQuote>> marketData,
            Dictionary<string, List<CqlQuote>> indicatorData,
            Dictionary<string, List<double>> profitData)
            : base("FX_6_2", stockid, 6, 1, new List<int>() { 2 })
        {
            _inputs.InputNeurons[0].Label = StockId;
            //_inputs.InputNeurons[1].Label = "EMA_2";
            _inputs.InputNeurons[1].Label = "EMA_10";
            //_inputs.InputNeurons[3].Label = "EMA_30";
            _inputs.InputNeurons[2].Label = "EMA_90";
            _inputs.InputNeurons[3].Label = "RSI_1";
            _inputs.InputNeurons[4].Label = "RSI_2";
            /*
            _inputs.InputNeurons[7].Label = "Trend_90";
            _inputs.InputNeurons[8].Label = "Trend_180";*/
            //_inputs.InputNeurons[7].Label = "WMVol_2";
            _inputs.InputNeurons[5].Label = "WMVol_10";
            //_inputs.InputNeurons[9].Label = "TrendVol_2";
            //_inputs.InputNeurons[10].Label = "TrendVol_10";
            _outputs.InputNeurons[0].Label = "decision";
            // build the neural network and the training set
            var quotes = new List<double>();
            foreach (var quote in marketData[StockId])
                quotes.Add((double)quote.MidPrice());
            var wma2 = new List<double>();
            foreach (var quote in indicatorData["EMA_2_" + StockId])
                wma2.Add((double)quote.MidPrice());
            var wma10 = new List<double>();
            foreach (var quote in indicatorData["EMA_10_" + StockId])
                wma10.Add((double)quote.MidPrice());
            var wma30 = new List<double>();
            foreach (var quote in indicatorData["EMA_30_" + StockId])
                wma30.Add((double)quote.MidPrice());
            var wma90 = new List<double>();
            foreach (var quote in indicatorData["EMA_90_" + StockId])
                wma90.Add((double)quote.MidPrice());
            var rsiShort = new List<double>();
            foreach (var quote in indicatorData["RSI_1_14_" + StockId])
                rsiShort.Add((double)quote.MidPrice());
            var rsiLong = new List<double>();
            foreach (var quote in indicatorData["RSI_2_14_" + StockId])
                rsiLong.Add((double)quote.MidPrice());
            var trendShort = new List<double>();
            foreach (var quote in indicatorData["Trend_90_14_" + StockId])
                trendShort.Add((double)quote.MidPrice());
            var trendLong = new List<double>();
            foreach (var quote in indicatorData["Trend_180_14_" + StockId])
                trendLong.Add((double)quote.MidPrice());
            var wmvolLow = new List<double>();
            foreach (var quote in indicatorData["WMVol_2_" + StockId])
                wmvolLow.Add((double)quote.MidPrice());
            var wmvolHigh = new List<double>();
            foreach (var quote in indicatorData["WMVol_10_" + StockId])
                wmvolHigh.Add((double)quote.MidPrice());
            var trendVolShort = new List<double>();
            foreach (var quote in indicatorData["Trend_30_6_WMVol_2_" + StockId])
                trendVolShort.Add((double)quote.MidPrice());
            var trendVolLong = new List<double>();
            foreach (var quote in indicatorData["Trend_60_6_WMVol_10_" + StockId])
                trendVolLong.Add((double)quote.MidPrice());
            for (int idxQuote = 0; idxQuote < quotes.Count; idxQuote++)
            {
                var newInputset = new List<double>();
                newInputset.Add(quotes[idxQuote]);
                //newInputset.Add(wma2[idxQuote]);
                newInputset.Add(wma10[idxQuote]);
                //newInputset.Add(wma30[idxQuote]);
                newInputset.Add(wma90[idxQuote]);
                newInputset.Add(rsiShort[idxQuote]);
                newInputset.Add(rsiLong[idxQuote]);
                /*
                newInputset.Add(trendShort[idxQuote]);
                newInputset.Add(trendLong[idxQuote]);*/
                //newInputset.Add(wmvolLow[idxQuote]);
                newInputset.Add(wmvolHigh[idxQuote]);
                //newInputset.Add(trendVolShort[idxQuote]);
                //newInputset.Add(trendVolLong[idxQuote]);
                _annInputs.Add(newInputset);
                var newOutputset = new List<double>();
                newOutputset.Add(profitData[StockId][idxQuote]);
                _annOutputs.Add(newOutputset);
            }
        }
    }
}
