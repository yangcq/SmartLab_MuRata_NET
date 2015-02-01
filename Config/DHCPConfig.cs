using SmartLab.MuRata.Type;

namespace SmartLab.MuRata.Config
{
    public class DHCPConfig
    {
        /*
         * 0: interface is assigned the static IP, NetMask and Gateway IP. First IP and Last IP are not present. Any active DHCP client or server is stopped.
         * 1: STA interface uses DHCP to obtain the address. All subsequent fields are not present. STA DHCP client is started if necessary.
         * 2: only for AP interface. If the soft AP is not started or SNIC_INIT_REQ is not done, this command fails. Otherwise, this command stops the HTTP server, DNS server and DHCP server if configured, and restarts them with new parameters. It assigns IP for clients in range [First IP, Last IP] within the subnet mask. The AP itself is assigned the address within the same subnet specified by IP which must not be in the range of [First IP, Last IP]. The value of GTW IP and IP should be the same. If there are clients connected to the soft AP before this command, make sure the clients reconnect to the soft AP after this command.
         */

        private WIFIInterface _interface;
        private DHCPMode mode;

        private IPAddress ip;
        private IPAddress mask;
        private IPAddress gateway;
        private IPAddress first;
        private IPAddress last;

        public DHCPConfig(WIFIInterface wifiInterface, DHCPMode mode)
        {
            this.SetDHCPMode(mode).SetInterface(wifiInterface);
        }

        public WIFIInterface GetInterface() { return this._interface; }
        public DHCPMode GetDHCPMode() { return this.mode; }
        public IPAddress GetLocalIP() { return this.ip; }
        public IPAddress GetNetmask() { return this.mask; }
        public IPAddress GetGatewayIP() { return this.gateway; }
        public IPAddress GetIPRangeFirst() { return this.first; }
        public IPAddress GetIPRangeLast() { return this.last; }

        public DHCPConfig SetInterface(WIFIInterface wifiInterface) 
        { 
            this._interface = wifiInterface;
            return this; 
        }

        public DHCPConfig SetDHCPMode(DHCPMode mode)
        { 
            this.mode = mode;
            return this; 
        }

        public DHCPConfig SetLocalIP(IPAddress ip)
        {
            this.ip = ip;
            return this; 
        }

        public DHCPConfig SetNetmask(IPAddress netmask)
        { 
            this.mask = netmask;
            return this; 
        }

        public DHCPConfig SetGatewayIP(IPAddress gateway)
        {
            this.gateway = gateway;
            return this; 
        }

        public DHCPConfig SetIPRange(IPAddress first, IPAddress last)
        {
            this.first = first;
            this.last = last;
            return this; 
        }
    }
}
