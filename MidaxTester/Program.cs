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
            // if there is a first argument "-G" then generate new results, otherwise test against the existing ones
            bool generate = (args.Length == 1 && args[0] == "-G");
            // test core functionalities
            Core.Run(generate);

            // test whole daily trading batches
            List<DateTime> tests = new List<DateTime>();
            tests.Add(new DateTime(2015, 12, 23));
            DailyReplay.Run(tests, generate);

            string statusSuccess = generate ? "Tests generated successfully" : "Tests passed successfully";
            Console.WriteLine(statusSuccess);

            MessageBox.Show(statusSuccess);
            
        }        
    }
}
