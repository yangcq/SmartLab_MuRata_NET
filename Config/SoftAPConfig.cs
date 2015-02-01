using SmartLab.MuRata.Type;

namespace SmartLab.MuRata.Config
{
    public class SoftAPConfig : WIFINetwork
    {
        /*
         * Parameters are as follows:
         * UINT8 Request Sequence
         * UINT8 Onoff
         * UINT8 Persistency
         * UINT8 SSID [up to 33]
         * UINT8 Channel
         * UINT8 Security mode
         * UINT8 Security key length (0-64)
         * … Security key [ ]
         * OnOff = 0 indicates AP is to be turned off. The rest of the parameters are ignored.
         * OnOff = 1 indicates turning on soft AP using existing NVM parameters,
         * OnOff = 2 indicates turning on AP with the parameters provided. If the soft AP is already on, it is first turned off.
         * Persistency=1 indicates the soft AP’s on/off state and parameters (if OnOff = 2) will be saved in NVM. For example, if OnOff =0 and Persistency=1, the soft AP will not be turned on after a reset.
         */

        public enum State
        {
            /// <summary>
            /// indicates AP is to be turned off. The rest of the parameters are ignored.
            /// </summary>
            OFF = 0x00,

            /// <summary>
            /// indicates turning on soft AP using existing NVM parameters,
            /// </summary>
            ON_NVM = 0x01,

            /// <summary>
            /// indicates turning on AP with the parameters provided. If the soft AP is already on, it is first turned off.
            /// </summary>
            ON_PARAMETERS = 0x02,
        }

        private State onOff;
        private bool persistency;

        /// <summary>
        /// OnOff = 0 indicates AP is to be turned off. The rest of the parameters are ignored.
        /// BSSID is not required
        /// !!! cannot be WEP and WIFI_SECURITY_WPA_AES_PSK !!!
        /// </summary>
        /// <param name="SSID">only required when OnOff = 2, which is ON_PARAMETERS</param>
        /// <param name="securityMode"></param>
        /// <param name="securityKey"></param>
        public SoftAPConfig(State state, string SSID = "", SecurityMode securityMode = SecurityMode.WIFI_SECURITY_OPEN, string securityKey = null)
            : base(SSID, securityMode, securityKey)
        {
            this.SetOnOffState(state);
        }

        public byte GetOnOffStatus() { return (byte)onOff; }

        public byte GetPersistency() { return (byte)(this.persistency ? 0x01 : 0x00); }

        public SoftAPConfig SetOnOffState(State onOff)
        {
            this.onOff = onOff;
            return this;
        }

        public SoftAPConfig SetPersistency(bool persistency)
        {
            this.persistency = persistency;
            return this;
        }

        public new SoftAPConfig SetSecurityKey(string SecurityKey)
        {
            base.SetSecurityKey(SecurityKey);
            return this;
        }

        public new SoftAPConfig SetBSSID(byte[] BSSID)
        {
            base.SetBSSID(BSSID);
            return this;
        }

        public new SoftAPConfig SetSSID(string SSID)
        {
            base.SetSSID(SSID);
            return this;
        }

        /// <summary>
        /// WIFI_SECURITY_OPEN
        /// WIFI_SECURITY_WPA_TKIP_PSK
        /// WIFI_SECURITY_WPA2_AES_PSK
        /// WIFI_SECURITY_WPA2_MIXED_PSK
        /// supported
        /// </summary>
        /// <param name="securityMode"></param>
        /// <returns></returns>
        public new SoftAPConfig SetSecurityMode(SecurityMode securityMode)
        {
            base.SetSecurityMode(securityMode);
            return this;
        }

        public new SoftAPConfig SetChannel(byte channel)
        {
            base.SetChannel(channel);
            return this;
        }
    }
}
