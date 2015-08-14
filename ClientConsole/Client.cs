using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Midax;

namespace ClientConsole
{
     public class Client
     {
         public class App : Ice.Application
         {
             private void menu()
             {
                 Console.WriteLine(
                     "usage:\n" +
                     "p: ping server\n" +
                     "start: start signals\n" +
                     "stop: stop signals\n" +
                     "s: status\n" +
                     "s: shutdown server\n" +
                     "x: exit\n" +
                     "?: help\n");
             }

             MidaxIcePrx serverController = null;

             public override int run(string[] args)
             {
                 if (args.Length > 0)
                 {
                     Console.Error.WriteLine(appName() + ": too many arguments");
                     return 1;
                 }

                 try
                 {
                     serverController = MidaxIcePrxHelper.checkedCast(communicator().stringToProxy("serverController"));
                 }
                 catch (Ice.NotRegisteredException)
                 {
                     IceGrid.QueryPrx query =
                         IceGrid.QueryPrxHelper.checkedCast(communicator().stringToProxy("MidaxIceGrid/Query"));
                     serverController = MidaxIcePrxHelper.checkedCast(query.findObjectByType("::Midax::MidaxIce"));
                 }
                 if (serverController == null)
                 {
                     Console.WriteLine("couldn't find a `::Midax::MidaxIce' object");
                     return 1;
                 }

                 menu();

                 string line = null;
                 do
                 {
                     try
                     {
                         Console.Write("==> ");
                         Console.Out.Flush();
                         line = Console.In.ReadLine();
                         if (line == null)
                         {
                             break;
                         }
                         if (line.Equals("p"))
                         {
                             Console.WriteLine(string.Format("{0} Ping", DateTime.Now.TimeOfDay));
                             Console.WriteLine(string.Format("{0} Answer: {1}", DateTime.Now.TimeOfDay, serverController.ping()));
                         }
                         else if (line.Equals("s"))
                         {
                             Console.WriteLine(string.Format("{0} Status:\n{1}", DateTime.Now.TimeOfDay, serverController.getStatus()));
                         }
                         else if (line.Equals("shut"))
                         {
                             serverController.shutdown();
                         }
                         else if (line.Equals("x"))
                         {
                             // Nothing to do
                         }
                         else if (line.Equals("?"))
                         {
                             menu();
                         }
                         else if (line.Equals("start"))
                         {
                             serverController.startsignals();
                         }
                         else if (line.Equals("stop"))
                         {
                             serverController.stopsignals();
                         }
                         else
                         {
                             Console.WriteLine("unknown command `" + line + "'");
                             menu();
                         }
                     }
                     catch (Ice.LocalException ex)
                     {
                         Console.WriteLine(ex);
                     }
                 }
                 while (!line.Equals("x"));

                 return 0;
             }
         }

         public static int Main(string[] args)
         {
             App app = new App();
             return app.main(args, "config.client");
         }
     }
}
