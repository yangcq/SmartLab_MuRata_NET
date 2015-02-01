using SmartLab.MuRata.ErrorCode;

namespace SmartLab.MuRata.Response
{
    public class InitializationResponse : Payload
    {
        public InitializationResponse(Payload payload)
            : base(payload)
        { }

        public SNICCode GetStatus() { return (SNICCode)this.GetData()[2]; }

        public int GetDefaultReceiveBufferSize() { return this.GetData()[3] << 8 | this.GetData()[4]; }

        public int GetMaximumUDPSupported() { return this.GetData()[5]; }

        public int GetMaximumTCPSupported() { return this.GetData()[6]; }
    }
}