using System;
using System.Runtime.InteropServices;
using System.ServiceProcess;

// Reference:
// https://docs.microsoft.com/en-us/dotnet/framework/windows-services/walkthrough-creating-a-windows-service-application-in-the-component-designer

/* TO DO
 * => Add the statistics necessary
 * => Add a timer feature for the statistics
 */

namespace ImproService
{
    static class Program
    {
        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        static void Main(string[] args)
        {
            ServiceBase[] services = new ServiceBase[] { new ImproService(args) };
            ServiceBase.Run(services);
        }
    }
}
