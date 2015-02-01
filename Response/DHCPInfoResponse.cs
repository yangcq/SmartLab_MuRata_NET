using SmartLab.MuRata.ErrorCode;
using SmartLab.MuRata.Type;

namespace SmartLab.MuRata.Response
{
    public class DHCPInfoResponse : Payload
    {
        public DHCPInfoResponse(Payload payload)
            : base(payload)
        { }

        public SNICCode GetStatus() { return (SNICCode)this.GetData()[2]; }

        public byte[] GetLocalMAC()
        {
            if (this.GetStatus() != SNICCode.SNIC_SUCCESS)
                return null;

            return new byte[] { this.GetData()[3], this.GetData()[4], this.GetData()[5], this.GetData()[6], this.GetData()[7], this.GetData()[8] };
        }

        public IPAddress GetLocalIP()
        {
            if (this.GetStatus() != SNICCode.SNIC_SUCCESS)
                return null;

            return new IPAddress(this.GetData(), 9);
        }

        public IPAddress GetGatewayIP()
        {
            if (this.GetStatus() != SNICCode.SNIC_SUCCESS)
                return null;

            return new IPAddress(this.GetData(), 13);
        }

        public IPAddress GetSubnetMask()
        {
            if (this.GetStatus() != SNICCode.SNIC_SUCCESS)
                return null;

            return new IPAddress(this.GetData(), 17);
        }

        public DHCPMode GetDHCPMode() { return (DHCPMode)this.GetData()[21]; }
    }
}
