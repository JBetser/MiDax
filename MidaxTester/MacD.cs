using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MidaxLib;

namespace MidaxTester
{
    public class MacD
    {
        public static void Run(List<DateTime> dates, bool generate = false, bool generate_from_db = false, bool publish_to_db = false, bool use_uat_db = false, bool fullday = false)
        {
            TestEngine testEngine = new TestEngine("MacD", dates, generate, generate_from_db, publish_to_db, use_uat_db, fullday);
            var models = new List<Model>();
            models.Add(new ModelMacDTest(new MarketData("DAX:IX.D.DAX.DAILY.IP"), 2, 10, 60));
            testEngine.Run(models); 
        }
    }
}
