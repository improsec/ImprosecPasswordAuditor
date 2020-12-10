using System;
using System.Collections.Generic;
using System.DirectoryServices.ActiveDirectory;
using System.Linq;
using System.Net.NetworkInformation;
using System.Text;
using System.Threading.Tasks;

namespace ImproService
{
    class Server
    {
        private static bool PingHost(string host)
        {
            using (Ping ping = new Ping())
            {
                try
                {
                    PingReply reply = ping.Send(host);
                    return (reply.Status == IPStatus.Success);
                }
                catch (PingException)
                {
                    return false;
                }
            }
        }

        public static string FindDomainController(string domain_name, ref string prev_ip, ref string prev_srv)
        {
            if (!string.IsNullOrEmpty(prev_ip))
            {
                if (PingHost(prev_ip))
                    return prev_srv;
            }

            var domain_context = new DirectoryContext(DirectoryContextType.Domain, domain_name);
            var domain_controllers = Domain.GetDomain(domain_context).FindAllDomainControllers();

            /* Perform two loops 
             * 1st: Check for self in domain controller list
             * 2nd: Check for any live host in domain controller list
             */
            for (int i = 0; i < 2; i++)
            {
                foreach (DomainController dc in domain_controllers)
                {
                    if (dc.IPAddress.Equals("::1") ||
                        dc.IPAddress.Equals("127.0.0.1") ||
                        (i != 0 && PingHost(dc.IPAddress)))
                    {
                        prev_ip = dc.IPAddress;

                        /* Return only the name of a domain controller rather than full domain path */
                        if (dc.Name.IndexOf('.') != -1)
                            return (prev_srv = dc.Name.Split('.')[0]);
                        else
                            return (prev_srv = dc.Name);
                    }
                }
            }

            return null;
        }
    }
}
