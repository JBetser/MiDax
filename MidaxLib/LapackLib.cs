using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NLapack;

namespace MidaxLib
{
    public class LapackLib
    {
        static NLapackLib _laPack = null;

        LapackLib()
        {
        }

        public static NLapackLib Instance { get { return _laPack == null ? _laPack = new NLapackLib() : _laPack; } }        
    }
}
