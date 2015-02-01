
namespace SmartLab.MuRata.Type
{
    public class WIFINetworkDetail : WIFINetwork
    {
        private int rssi;

        private BSSType netType;

        // Max Data Rate (Mbps)
        private int maxDataRate;

        public WIFINetworkDetail() { }

        public WIFINetworkDetail(string SSID, SecurityMode securityMode, BSSType networkType, int rssi, int maxDataRate)
            : base(SSID, securityMode)
        {
            this.netType = networkType;
            this.rssi = rssi;
            this.maxDataRate = maxDataRate;
        }

        public int GetRSSI() { return this.rssi; }

        /// <summary>
        /// Max Data Rate (Mbps)
        /// </summary>
        /// <returns></returns>
        public int GetMaxDataRate() { return this.maxDataRate; }

        public BSSType GetNetworkType() { return this.netType; }

        public WIFINetworkDetail SetRSSI(int rssi)
        {

            if (rssi >> 7 == 0x01)
                this.rssi = (~(rssi - 1) & 0x7F) * -1;
            else
                this.rssi = rssi;
            return this;
        }

        public WIFINetworkDetail SetNetworkType(BSSType networkType) { this.netType = networkType; return this; }

        public WIFINetworkDetail SetMaxDataRate(int maxDataRate) { this.maxDataRate = maxDataRate; return this; }

        public new WIFINetworkDetail SetSecurityKey(string SecurityKey)
        {
            base.SetSecurityKey(SecurityKey);
            return this;
        }

        public new WIFINetworkDetail SetBSSID(byte[] BSSID)
        {
            base.SetBSSID(BSSID);
            return this;
        }

        public new WIFINetworkDetail SetSSID(string SSID)
        {
            base.SetSSID(SSID);
            return this;
        }

        public new WIFINetworkDetail SetSecurityMode(SecurityMode securityMode)
        {
            base.SetSecurityMode(securityMode);
            return this;
        }

        public new WIFINetworkDetail SetChannel(byte channel)
        {
            base.SetChannel(channel);
            return this;
        }

    }
}