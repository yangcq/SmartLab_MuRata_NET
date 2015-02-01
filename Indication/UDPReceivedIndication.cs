using System;
using SmartLab.MuRata.Type;

namespace SmartLab.MuRata.Indication
{
    /// <summary>
    /// This event is generated when a UDP server (in unconnected mode) receives a packet.
    /// </summary>
    public class UDPReceivedIndication : Payload
    {
        public const int PAYLOAD_OFFSET = 11;

        private int receiveLength;

        public UDPReceivedIndication(Payload payload)
            : base(payload)
        {
            receiveLength = this.GetData()[9] << 8 | this.GetData()[10];
        }

        public byte GetServerSocketID() { return this.GetData()[2]; }

        public IPAddress GetRemoteIP() { return new IPAddress(this.GetData(), 3); }

        public int GetRemotePort() { return this.GetData()[7] << 8 | this.GetData()[8]; }

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