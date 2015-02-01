using System;

namespace SmartLab.MuRata.Indication
{
    /// <summary>
    /// This event is generated when a TCP server or a UDP server (in connected mode) receives a packet. Since there is no client address and port information, the application may need to call
    /// </summary>
    public class SocketReceiveInidcation : Payload
    {
        public const int PAYLOAD_OFFSET = 5;

        private int receiveLength;

        public SocketReceiveInidcation(Payload payload)
            : base(payload)
        {
            receiveLength = this.GetData()[3] << 8 | this.GetData()[4];
        }

        public byte GetServerSocketID() { return this.GetData()[2]; }

        public int GetPayloadLength() { return this.receiveLength; }

        public byte GetPayload(int index) { return this.GetData()[index + PAYLOAD_OFFSET]; }

        public byte[] GetPayload()
        {
            byte[] con = new byte[this.receiveLength];
            Array.Copy(this.GetData(), PAYLOAD_OFFSET, con, 0, this.receiveLength);
            return con;
        }

        /// <summary>
        /// Get the start index of the payload
        /// </summary>
        /// <returns></returns>
        public int GetPayloadOffset() { return PAYLOAD_OFFSET; }
    }
}
