using System; 
using System.Diagnostics; 
using System.ComponentModel; 
using System.Configuration.Install; 

namespace EventLogSourceInstaller 
{ 
    [RunInstaller(true)] 
    public class IGEventLogInstaller : Installer { 
        
        private EventLogInstaller eventLogInstaller; 
        
        public IGEventLogInstaller() { //Create Instance of EventLogInstaller 
            eventLogInstaller = new EventLogInstaller(); // Set the Source of Event Log, to be created. 
            eventLogInstaller.Source = "BPModel"; // Set the Log that source is created in 
            eventLogInstaller.Log = "Midax"; // Add myEventLogInstaller to the Installers Collection. 
            Installers.Add(eventLogInstaller);
        } 
    } 
}