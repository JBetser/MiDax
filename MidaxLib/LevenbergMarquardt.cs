using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NLapack;
using NLapack.Matrices;
using NLapack.Numbers;

namespace MidaxLib
{
    public class Value
    {
        public double X = 0.0;

        public Value(double val = 0.0)
        {
            X = val;
        }
    }

    public class LevenbergMarquardt
    {
        public delegate NRealMatrix objective_func(NRealMatrix input);
        public delegate NRealMatrix model_func(NRealMatrix input, NRealMatrix weights);
        
        objective_func _obj_func;
        model_func _model;
        model_func _jac;
        NRealMatrix _inputs;
        NRealMatrix _outputs;
        NRealMatrix _weights;
        NRealMatrix _error;
        double _obj_error;
        double _totalError;
        double _lambda;
        int _max_iter;
        List<Value> _modelParams;  // The results are updated here

        public double ObjectiveError { get { return _obj_error; } }

        public LevenbergMarquardt(objective_func obj_func, List<double> inputs, List<Value> modelParams, model_func model, model_func model_jac, double lambda = 0.001, double obj_error = 0.00001, int max_iter = 100)
        {
            if (inputs.Count == 0)
                throw new ApplicationException("Number of input data must be > 0");
            if (modelParams.Count == 0)
                throw new ApplicationException("Number of model parameters must be > 0");
            _obj_func = obj_func;
            _model = model;
            _jac = model_jac;
            _lambda = lambda;
            _max_iter = max_iter;
            _inputs = new NRealMatrix(inputs.Count, 1);
            _inputs.SetArray((from input in inputs select new NDouble[] { new NDouble(input) } ).ToArray());
            _outputs = _obj_func(_inputs);
            _modelParams = modelParams;

            // initalize the weights with normal random distibution
            var seed = new MLapack.MCJIMatrix(4,1);
            seed.setAt(0, 0, 123);
            seed.setAt(1, 0, 123);
            seed.setAt(2, 0, 123);
            seed.setAt(3, 0, 123);

            // check if a guess has been provided
            bool modelParamInitialized = false;
            foreach (var weight in modelParams)
            {
                if (weight.X != 0)
                    modelParamInitialized = true;
            }
            if (modelParamInitialized)
            {
                _weights = new NRealMatrix(modelParams.Count, 1);
                _weights.SetArray((from param in modelParams select new NDouble[] { new NDouble(param.X) }).ToArray());
            }
            else
                _weights = LapackLib.Instance.RandomMatrix(RandomDistributionType.Normal, seed, modelParams.Count, 1);

            _obj_error = obj_error;
            _error = calcError(_weights);
            _totalError = calcTotalError(_error);
        }

        public void Solve()
        {
            int nbIter = 0;
            while (_totalError > _obj_error)
            {
                if (nbIter++ > _max_iter)
                    throw new ApplicationException("LevenbergMarquardt cannot converge within maximum number of iterations"); 
                
                nextStep();

                for (int idxParam = 0; idxParam < _weights.Rows; idxParam++)
                    _modelParams[idxParam].X = _weights[idxParam, 0];
            }            
        }

        NRealMatrix calcError(NRealMatrix weights)
        {
            var error = new NRealMatrix(weights.Rows, 1);
            NRealMatrix modelOutput = _model(_inputs, weights);
            for (int idxError = 0; idxError < error.Rows; idxError++)
                error.SetAt(idxError, 0, new NDouble(_outputs[idxError, 0] - modelOutput[idxError, 0]));
            return error;
        }

        double calcTotalError(NRealMatrix error)
        {
            double sumSquare = 0;
            for (int idxError = 0; idxError < error.Rows; idxError++)
                sumSquare += error[idxError, 0] * error[idxError, 0];
            return Math.Sqrt(sumSquare);
        }
        
        void nextStep()
        {
            // compute jacobian
            var jac = _jac(_inputs, _weights);
            // compute hessian approximation with tykhonov damping coefficient
            var jacT = new NRealMatrix(jac.Rows, jac.Columns);
            jacT.SetArray(jac.ToArray());
            jacT.Transpose();
            var dampedHessian = new NRealMatrix(jac.Columns, jac.Columns);
            dampedHessian = jacT * jac;
            for (int idxRow = 0; idxRow < dampedHessian.Rows; idxRow++)
                dampedHessian.SetAt(idxRow, idxRow, new NDouble(dampedHessian[idxRow, idxRow] + _lambda));            
            var adj = new NRealMatrix(dampedHessian.Columns, 1);
            var y = new NRealMatrix(dampedHessian.Rows, 1);
            y = jacT * _error;
            // solve dampedHessian * adj = y
            LapackLib.Instance.SolveSle(dampedHessian, y, adj);
            var nextWeights = new NRealMatrix(_weights.Rows, 1);
            for (int idxWeight = 0; idxWeight < nextWeights.Rows; idxWeight++)
                nextWeights.SetAt(idxWeight, 0, new NDouble(_weights[idxWeight, 0] - adj[idxWeight, 0]));
            // compute errors
            var error = calcError(nextWeights);
            var totalError = calcTotalError(error);
            if (totalError > _totalError)
            {
                // revert step and increase damping factor
                _lambda *= 10;
            }
            else
            {
                // accept step and decrease damping factor
                _lambda /= 10;
                _weights.SetArray(nextWeights.ToArray());
                _error = error;
                _totalError = totalError;
            }
        }

    }
}
