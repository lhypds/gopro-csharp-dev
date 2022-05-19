using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.NetworkInformation;
using System.Text;
using System.Threading.Tasks;

namespace GoProCSharpDev.Utils
{
    class NetUtils
    {
        public static bool Ping(string host)
        {
            Ping pingSender = new Ping();
            PingReply reply = pingSender.Send(host);
            if (reply.Status == IPStatus.Success)
            {
                Console.WriteLine("Address: {0}", reply.Address.ToString());
                Console.WriteLine("RoundTrip time: {0}", reply.RoundtripTime);
                Console.WriteLine("Time to live: {0}", reply.Options.Ttl);
                Console.WriteLine("Don't fragment: {0}", reply.Options.DontFragment);
                Console.WriteLine("Buffer size: {0}", reply.Buffer.Length);
                return true;
            }
            else if (reply.Status == IPStatus.DestinationPortUnreachable)
            {
                Console.WriteLine("Address: {0}", reply.Status);
                return true;
            }
            else
            {
                Console.WriteLine("Address: {0}", reply.Status);
                return false;
            }
        }
    }
}
