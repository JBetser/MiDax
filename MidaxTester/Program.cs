using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using MidaxLib;

namespace MidaxTester
{   
    class Program
    {
        static void Main(string[] args)
        {
            // test core functionalities
            Core.Run();

            // test whole daily trading batches
            List<DateTime> tests = new List<DateTime>();
            tests.Add(new DateTime(2015, 12, 23));
            DailyReplay.Run(tests);

            string statusSuccess = "Tests passed successfully";
            Console.WriteLine(statusSuccess);

            MessageBox.Show(statusSuccess);
            
        }        
    }
}
