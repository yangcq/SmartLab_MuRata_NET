using SmartLab.MuRata.Type;

namespace SmartLab.MuRata
{
    /*
     * 7 | 6 | 5 4 3 2 1 0
     *         SOM(0x02)
     * 1 |       L0
     * 1 | A |    L1
     * 1 |   Command ID
     *       Payload
     *       ...
     *       ...
     * 1 |   Checksum
     *         EOM(0x04)
     */

    /*
     * Each frame is delineated by a Start of Message (SOM) and End of Message (EOM) byte. The rest of the
     * fields are as follows:
     * The frame starts with a SOM (0x02) and ends with an EOM (0x04). The SOM and EOM values
     * may also appear in application payload.
     * Payload length (L1:L0): octet length of application payload including any escape characters. L0
     * stands for bit0 to bit6, and L1 stands for bit7 to bit12 of the payload length.
     * Command ID: specifies types of payload
     * A: 1 if ACK required, 0 if no ACK is required. If A=1, then the receiver must send ACK upon a
     * successful validation of the frame. A frame requiring acknowledgement must be acknowledged
     * Murata SW Design
     * Murata Proprietary Page 11 of 101
     * before any other non-response frame may be transmitted, i.e., a command response is always permitted.
     * Checksum: sum of L0, A | L1 and command ID.
     */
    public class UARTFrame
    {
        public static readonly byte SOM = 0x02;
        public static readonly byte EOM = 0x04;

        private byte l0;
        private byte l1;

        private bool needACK;

        private CommandID commandid;

        private Payload payload;

        private byte checksum;

        // length
        public byte GetL0() { return l0; }

        public void SetL0(int value) { this.l0 = (byte)(value & 0x7F); }

        public byte GetL1() { return l1; }

        public void SetL1(int value)
        {
            this.l1 = (byte)(value & 0x3F);

            if ((value & 0x40) == 0x40)
                this.needACK = true;
            else this.needACK = false;
        }

        public int GetPayloadLength() { return (l1 << 7) | l0; }

        public void SetPayloadLength(int length)
        {
            this.SetL0(length);
            this.SetL1(length >> 7);
        }

        // ack
        public bool GetACKRequired() { return this.needACK; }

        public void SetACKRequired(bool ack) { this.needACK = ack; }

        //command id
        public CommandID GetCommandID() { return this.commandid; }

        public void SetCommandID(CommandID id) { this.commandid = id; }

        public void SetCommandID(int value) { this.commandid = (CommandID)(value & 0x7F); }

        // payload
        public void SetPayload(Payload payload)
        {
            this.payload = payload;
            this.SetPayloadLength(payload.GetPosition());
            this.SetChecksum(l0 + (needACK ? l1 | 0x40 : l1) + (byte)commandid);
        }

        public Payload GetPayload() { return this.payload; }

        // checksum
        public byte GetChecksum() { return this.checksum; }

        public void SetChecksum(int checksum) { this.checksum = (byte)(checksum & 0x7F); }

        public bool VerifyChecksum()
        {
            if (((l0 + (needACK ? l1 | 0x40 : l1) + (byte)commandid) & 0x7F) == this.checksum)
                return true;
            return false;
        }
    }
}