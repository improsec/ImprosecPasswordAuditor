using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Configuration.Install;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;

namespace ImproService
{
    [RunInstaller(true)]
    public partial class ProjectInstaller : System.Configuration.Install.Installer
    {
        public ProjectInstaller()
        {
            InitializeComponent();

            foreach (Installer installer in serviceInstaller1.Installers)
            {
                if (installer is EventLogInstaller)
                {
                    serviceInstaller1.Installers.Remove(installer);
                    break;
                }
            }

            EventLogInstaller log_installer = new EventLogInstaller
            {
                Source = Config.EventLogSource,
                Log = Config.EventLogName
            };

            this.Installers.Add(log_installer);
        }
    }
}
