using SmartLab.MuRata.ErrorCode;

namespace SmartLab.MuRata.Response
{
    public class SendFromSocketResponse : Payload
    {
        public SendFromSocketResponse(Payload payload)
            : base(payload)
        { }

        public SNICCode GetStatus() { return (SNICCode)this.GetData()[2]; }

        public int GetNumberofBytesSent()
        {
            if (this.GetStatus() == SNICCode.SNIC_SUCCESS)
                return this.GetData()[3] << 8 | this.GetData()[4];

            return -1;
        }
    }
}
