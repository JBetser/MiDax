using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Windows.Forms.DataVisualization.Charting;

namespace MidaxLib
{
    public class AnnVisualizer : IDisposable
    {
        AnnVisualizerForm _form = null;
        Thread _thread;

        public AnnVisualizer(NeuralNetwork ann)
        {
            _thread = new Thread(() => RealStart(this, ann));
            _thread.Start();
        }

        public void Show(NeuralNetwork ann)
        {
            _form = new AnnVisualizerForm(ann);
            _form.Show();
        }

        public void Update()
        {
            _form.Invalidate();
            foreach (Control control in _form.Controls)
                control.Invalidate();
            _form.Recompute();            
            Application.DoEvents();
        }
        
        private static void RealStart(AnnVisualizer thisVisualizer, NeuralNetwork ann)
        {
            thisVisualizer.Show(ann);

            int refreshCount = 0;
            while (true)
            {
                if (refreshCount++ == 2)
                {
                    refreshCount = 0;
                    thisVisualizer.Update();
                }
                Thread.Sleep(200);
            }
        }

        // Dispose() calls Dispose(true)
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        // NOTE: Leave out the finalizer altogether if this class doesn't 
        // own unmanaged resources itself, but leave the other methods
        // exactly as they are. 
        ~AnnVisualizer() 
        {
            // Finalizer calls Dispose(false)
            Dispose(false);
        }

        // The bulk of the clean-up code is implemented in Dispose(bool)
        protected virtual void Dispose(bool disposing)
        {
            if (disposing) 
            {
                // free managed resources
                if (_form != null)
                {
                    _form.Invoke(new Action(() => _form.Close()));
                    _form = null;
                }
                if (_thread != null)
                {
                    _thread.Abort();
                    _thread = null;
                }
            }
        }
    }
}
