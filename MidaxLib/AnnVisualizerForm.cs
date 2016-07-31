using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Windows.Forms.DataVisualization.Charting;

namespace MidaxLib
{
    public partial class AnnVisualizerForm : Form
    {
        NeuralNetwork _ann;
        Neuron _transferFunc;
        int _neuronWidth = 110;
        int _neuronHeight = 130;

        public AnnVisualizerForm(NeuralNetwork ann)
        {
            _ann = ann;
            InitializeComponent();
            ResizeEnd += AnnVisualizerForm_Resize;
            Paint += AnnVisualizerForm_Paint;

            var tlayer = new NeuralLayer(1);
            Neuron input = new NeuronInput(tlayer, "X");            
            tlayer.Neurons.Add(input);
            _transferFunc = new Neuron(tlayer, new NeuralLayer(0), "transfer");
            _transferFunc.Weights[0].X = 1.0;

            Recompute();
        }

        Label findWeightLabelOrig(int idxNeuron)
        {
            foreach (var control in _neuronUIcontrols)
            {
                if (control.Name == "labelW" + idxNeuron.ToString())
                    return (Label)control;
            }
            return null;
        }

        Label findWeightLabelDest(int idxNeuron)
        {
            foreach (var control in _neuronUIcontrols)
            {
                if (control.Name == "label" + idxNeuron.ToString())
                    return (Label)control;
            }
            return null;
        }

        Chart findChart(int idxNeuron)
        {
            foreach (var control in _neuronUIcontrols)
            {
                if (control.Name == "chart" + idxNeuron.ToString())
                    return (Chart)control;
            }
            return null;
        }

        void AnnVisualizerForm_Paint(object sender, PaintEventArgs e)
        {
            Pen pen = new Pen(Color.FromArgb(255, 0, 0, 0));            
            int idxNeuron = 0;
            Label curDest = null;
            int idxLayer = 0;
            int nextLayerIdx = _ann._outputs.Neurons.Count;
            foreach (var nn in _ann._outputs.Neurons)
            {
                var nextOrigin = findWeightLabelOrig(idxNeuron++);
                int idxWeight = 0;
                Value max = nn.Weights.Max();
                foreach (var nextNn in _ann._innerLayers[0].Neurons)
                {
                    curDest = findWeightLabelDest(nextLayerIdx++);
                    double strength = Math.Abs(nn.Weights[idxWeight++].X / max.X);
                    pen.Color = Color.FromArgb(255, (byte)(strength * 255.0), 0, (byte)((1.0 - strength) * 255.0));
                    e.Graphics.DrawLine(pen, nextOrigin.Location.X, nextOrigin.Location.Y, curDest.Location.X, curDest.Location.Y + 10);
                }
            }
            foreach (var layer in _ann._innerLayers)
            {
                int idxCurLayerNn = idxNeuron + layer.Neurons.Count;
                foreach (var nn in layer.Neurons)
                {
                    var nextOrigin = findWeightLabelOrig(idxNeuron++);
                    NeuralLayer nextLayer = null;
                    if (_ann._innerLayers.Count > idxLayer + 1)
                        nextLayer = _ann._innerLayers[idxLayer + 1];
                    else
                        nextLayer = _ann._inputs;
                    nextLayerIdx = idxCurLayerNn;                    
                    if (nn.Weights.Count > 0)
                    {
                        Value max = nn.Weights.Max();
                        int idxWeight = 0;
                        foreach (var nextNn in nextLayer.Neurons)
                        {
                            curDest = findWeightLabelDest(nextLayerIdx++);
                            double strength = Math.Abs(nn.Weights[idxWeight++].X / max.X);
                            pen.Color = Color.FromArgb(255, (byte)(strength * 255.0), 0, (byte)((1.0 - strength) * 255.0));
                            e.Graphics.DrawLine(pen, nextOrigin.Location.X, nextOrigin.Location.Y, curDest.Location.X, curDest.Location.Y + 10);
                        }
                    }
                }
                idxLayer++;
            }
        }

        public void Recompute()
        {
            UpdateControl updateFunc = null;
            if (_neuronUIcontrols.Count == 0)
                updateFunc = addNeuron;
            else
                updateFunc = updateNeuron;

            int curOffsetX = (Size.Width - _ann._outputs.Neurons.Count * _neuronWidth) / (_ann._outputs.Neurons.Count + 1);
            int curOffsetY = (Size.Height - (_ann._innerLayers.Count + 2) * _neuronHeight) / (_ann._innerLayers.Count + 3);
            int curX = curOffsetX;
            int curY = curOffsetY;
            int idxNeuron = 0;
            foreach (var nn in _ann._outputs.Neurons)
            {                
                updateFunc(nn, curX + _ann._outputs.Neurons.IndexOf(nn) * _neuronWidth, curY, idxNeuron++);
                curX += curOffsetX;
            }
            foreach (var layer in _ann._innerLayers)
            {
                curOffsetX = (Size.Width - layer.Neurons.Count * _neuronWidth) / (layer.Neurons.Count + 1);
                curX = curOffsetX;
                curY += _neuronHeight + curOffsetY;
                foreach (var nn in layer.Neurons)
                {                    
                    updateFunc(nn, curX + layer.Neurons.IndexOf(nn) * _neuronWidth, curY, idxNeuron++);
                    curX += curOffsetX;
                }
            }
            curOffsetX = (Size.Width - _ann._inputs.Neurons.Count * _neuronWidth) / (_ann._inputs.Neurons.Count + 1);
            curX = curOffsetX;
            curY += _neuronHeight + curOffsetY;
            foreach (var nn in _ann._inputs.Neurons)
            {
                updateFunc(nn, curX + _ann._inputs.Neurons.IndexOf(nn) * _neuronWidth, curY, idxNeuron++);
                curX += curOffsetX;
            }

        }

        private double transferFunc(double i)
        {
            _transferFunc.Children.InputNeurons[0].Value.X = i / 10.0;
            _transferFunc.Updated = false;
            _transferFunc.Activation();
            return _transferFunc.Value.X;
        }

        List<Control> _neuronUIcontrols = new List<Control>();

        void addNeuron(Neuron nn, int X, int Y, int idxNeuron)
        {
            var label = new Label();
            label.AutoSize = true;
            label.Location = new System.Drawing.Point(X, Y);
            label.Name = "label" + idxNeuron.ToString();
            label.Size = new System.Drawing.Size(35, 13);
            label.TabIndex = 0;
            label.Text = nn.Label;
            Controls.Add(label);
            _neuronUIcontrols.Add(label);

            if (nn.GetType() == typeof(NeuronInput) || nn.GetType() == typeof(NeuronBias))
                return;

            System.Windows.Forms.DataVisualization.Charting.ChartArea chartArea1 = new System.Windows.Forms.DataVisualization.Charting.ChartArea();
            var chart = new System.Windows.Forms.DataVisualization.Charting.Chart();
            chartArea1.Name = "ChartArea" + idxNeuron.ToString();
            chartArea1.AxisX.LabelAutoFitMaxFontSize = 5;
            chartArea1.AxisX.Interval = 30.0;
            chartArea1.AxisX.MinorTickMark.Size = 30;
            chartArea1.AxisY.LabelAutoFitMaxFontSize = 5;
            chartArea1.AxisY.Interval = 3.0;
            chartArea1.AxisX.MajorGrid.Enabled = false;
            chartArea1.AxisX.MinorGrid.Enabled = false;
            chartArea1.AxisY.MajorGrid.Enabled = false;
            chartArea1.AxisY.MinorGrid.Enabled = false;
            chart.ChartAreas.Add(chartArea1);
            chart.Location = new System.Drawing.Point(X, Y + 15);
            chart.Name = "chart" + idxNeuron.ToString();
            chart.Size = new System.Drawing.Size(_neuronWidth - 10, _neuronWidth - 10);
            chart.TabIndex = 0;
            chart.Series.Clear();
            var series1 = new System.Windows.Forms.DataVisualization.Charting.Series
            {
                Name = "Series1",
                Color = System.Drawing.Color.LightGray,
                IsVisibleInLegend = false,
                IsXValueIndexed = false,
                ChartType = SeriesChartType.Line
            };
            chart.Series.Add(series1);
            for (double x = -50; x < 50; x++)
                series1.Points.AddXY(x, transferFunc(x));
            var series2 = new System.Windows.Forms.DataVisualization.Charting.Series
            {
                Name = "Series2",
                Color = nn.FirstInputValue < nn.InputValue ? System.Drawing.Color.Red : System.Drawing.Color.Blue,
                IsVisibleInLegend = false,
                IsXValueIndexed = false,
                ChartType = SeriesChartType.Line
            };
            chart.Series.Add(series2);
            double begin = nn.FirstInputValue < nn.InputValue ? nn.FirstInputValue : nn.InputValue;
            double end = nn.FirstInputValue < nn.InputValue ? nn.InputValue : nn.FirstInputValue;
            for (double x = begin; x < end + 1.0; x++)
                series2.Points.AddXY(x, transferFunc(x));
            Controls.Add(chart);
            _neuronUIcontrols.Add(chart);

            var labelW = new Label();
            labelW.AutoSize = true;
            labelW.Location = new System.Drawing.Point(X, Y + _neuronHeight - 15);
            labelW.Name = "labelW" + idxNeuron.ToString();
            labelW.Size = new System.Drawing.Size(_neuronWidth, 13);
            labelW.TabIndex = 0;
            labelW.Font = new Font(labelW.Font.FontFamily, 7.0f);
            foreach(var w in nn.Weights)
                labelW.Text += string.Format("{0:F2};", w.X);
            if (nn.Weights.Count > 0)
                labelW.Text = labelW.Text.Substring(0, labelW.Text.Length - 1);
            Controls.Add(labelW);
            _neuronUIcontrols.Add(labelW);            
        }

        void updateNeuron(Neuron nn, int X, int Y, int idxNeuron)
        {
            Label labelW = findWeightLabelOrig(idxNeuron);
            if (labelW != null)
            {
                labelW.Text = "";
                foreach (var w in nn.Weights)
                    labelW.Text += string.Format("{0:F2};", w.X);
                if (nn.Weights.Count > 0)
                    labelW.Text = labelW.Text.Substring(0, labelW.Text.Length - 1);
            }            
            Chart chart = findChart(idxNeuron);
            if (chart != null)
            {
                chart.Series["Series2"].Points.Clear();
                chart.Series["Series2"].Color = nn.FirstInputValue < nn.InputValue ? System.Drawing.Color.Red : System.Drawing.Color.Blue;
                double begin = nn.FirstInputValue < nn.InputValue ? nn.FirstInputValue : nn.InputValue;
                double end = nn.FirstInputValue < nn.InputValue ? nn.InputValue : nn.FirstInputValue;
                for (double x = begin; x < end + 1.0; x++)
                    chart.Series["Series2"].Points.AddXY(x, transferFunc(x));
            }
        }

        delegate void UpdateControl(Neuron nn, int X, int Y, int idxNeuron);

        private void AnnVisualizerForm_Resize(object sender, System.EventArgs e)
        {            
            foreach (var control in _neuronUIcontrols)
                Controls.Remove(control);
            _neuronUIcontrols.Clear();
            Recompute();
        }
    }
}
