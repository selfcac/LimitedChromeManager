using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace Socket2Process
{
    public static class IpAddrUtils
    {
        // http://www.java2s.com/Code/CSharp/Network/IPtoUint.htm

        public static uint IpToUint(IPAddress ip, bool IsLittleEndian = true)
        {
            byte[] bytes = null;
            uint uip = 0;
            if (IsLittleEndian)
            {
                bytes = ip.GetAddressBytes();
            }
            else
            {
                bytes = ip.GetAddressBytes().Reverse().ToArray();
            }
            uip = (uint)((bytes[0] << 24) | (bytes[1] << 16) | (bytes[2] << 8) | bytes[3]);
            return uip;
        }

        public static IPAddress UintToIP(uint ipaddr, bool reverse = false, bool IsLittleEndian = true)
        {
            //fix endianess from network
            if (IsLittleEndian)
            {
                ipaddr = (
                        (ipaddr << 24) & 0xFF000000) +
                        ((ipaddr << 8) & 0x00FF0000) +
                        ((ipaddr >> 8) & 0x0000FF00) +
                        ((ipaddr >> 24) & 0x000000FF
                      );
            }
            else
            {
                ipaddr = (
                        ((ipaddr >> 24) & 0x000000FF +
                        ((ipaddr >> 8) & 0x0000FF00) +
                        ((ipaddr << 8) & 0x00FF0000) +
                        (ipaddr << 24) & 0xFF000000)  
                     );
            }

            var result =  new System.Net.IPAddress(ipaddr);
            if (reverse)
                result = new IPAddress(result.GetAddressBytes().Reverse().ToArray());
            return result;
        }


    }
}
