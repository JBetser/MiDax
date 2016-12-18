using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MidaxLib;

namespace Calibrator
{
    class NeuralNetworkWatershed_1_15 : NeuralNetworkForCalibration
    {
        public NeuralNetworkWatershed_1_15(string stockid,
            Dictionary<string, List<CqlQuote>> marketData,
            Dictionary<string, List<CqlQuote>> indicatorData,
            Dictionary<string, List<double>> profitData)
            : base("Water_1_15", stockid, 5 + 15 * 4, 1, new List<int>() { 4, 16 })
        {
            _inputs.InputNeurons[0].Label = StockId;
            _inputs.InputNeurons[1].Label = "EMA_10";
            _inputs.InputNeurons[2].Label = "EMA_30";
            _inputs.InputNeurons[3].Label = "EMA_90";
            _inputs.InputNeurons[4].Label = "RSI_3";
            for (int idxPool = 0; idxPool < 15; idxPool++)
            {
                _inputs.InputNeurons[5 + idxPool * 3].Label = "WD_" + idxPool;
                _inputs.InputNeurons[5 + idxPool * 3 + 1].Label = "WV_" + idxPool;
                _inputs.InputNeurons[5 + idxPool * 3 + 2].Label = "WT_" + idxPool;
                _inputs.InputNeurons[5 + idxPool * 3 + 3].Label = "WR_" + idxPool;
            }
            _outputs.InputNeurons[0].Label = "decision";
            // build the neural network and the training set
            var quotes = new List<double>();
            foreach (var quote in marketData[StockId])
                quotes.Add((double)quote.MidPrice());
            var wma10 = new List<double>();
            foreach (var quote in indicatorData["EMA_10_" + StockId])
                wma10.Add((double)quote.MidPrice());
            var wma30 = new List<double>();
            foreach (var quote in indicatorData["EMA_30_" + StockId])
                wma30.Add((double)quote.MidPrice());
            var wma90 = new List<double>();
            foreach (var quote in indicatorData["EMA_90_" + StockId])
                wma90.Add((double)quote.MidPrice());
            var rsi3 = new List<double>();
            foreach (var quote in indicatorData["RSI_3_" + StockId])
                rsi3.Add((double)quote.MidPrice());
            var wd = new List<List<double>>();
            var wv = new List<List<double>>();
            var wt = new List<List<double>>();
            var wr = new List<List<double>>();
            for (int idxPool = 0; idxPool < 15; idxPool++)
            {
                var wd_cur = new List<double>();
                var wv_cur = new List<double>();
                var wt_cur = new List<double>();
                var wr_cur = new List<double>();
                foreach (var quote in indicatorData["Water_1_15_depth" + idxPool + "_" + StockId])
                    wd_cur.Add((double)quote.MidPrice());
                foreach (var quote in indicatorData["Water_1_15_valuediff" + idxPool + "_" + StockId])
                    wv_cur.Add((double)quote.MidPrice());
                foreach (var quote in indicatorData["Water_1_15_timediff" + idxPool + "_" + StockId])
                    wt_cur.Add((double)quote.MidPrice());
                foreach (var quote in indicatorData["Water_1_15_rsi" + idxPool + "_" + StockId])
                    wr_cur.Add((double)quote.MidPrice());
                wd.Add(wd_cur);
                wv.Add(wv_cur);
                wt.Add(wt_cur);
                wr.Add(wr_cur);
            }
            for (int idxQuote = 0; idxQuote < quotes.Count; idxQuote++)
            {
                var newInputset = new List<double>();
                newInputset.Add(quotes[idxQuote]);
                newInputset.Add(wma10[idxQuote]);
                newInputset.Add(wma30[idxQuote]);
                newInputset.Add(wma90[idxQuote]);
                newInputset.Add(rsi3[idxQuote]);
                for (int idxPool = 0; idxPool < 15; idxPool++)
                {
                    newInputset.Add(wd[idxPool][idxQuote]);
                    newInputset.Add(wv[idxPool][idxQuote]);
                    newInputset.Add(wt[idxPool][idxQuote]);
                    newInputset.Add(wr[idxPool][idxQuote]);
                }
                _annInputs.Add(newInputset);
                var newOutputset = new List<double>();
                newOutputset.Add(profitData[StockId][idxQuote]);
                _annOutputs.Add(newOutputset);
            }
        }
    }
}
