using SmartLab.MuRata.ErrorCode;

namespace SmartLab.MuRata.Response
{
    public class SocketStartReceiveResponse : Payload
    {
        public SocketStartReceiveResponse(Payload payload)
            : base(payload)
        { }

        public SNICCode GetStatus() { return (SNICCode)this.GetData()[2]; }

        public int GetReceiveBufferSize()
        {
            if (GetStatus() == SNICCode.SNIC_SUCCESS)
                return this.GetData()[3] << 8 | this.GetData()[4];

            return -1;
        }
    }
}