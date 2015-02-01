using System;
using System.Text;
using SmartLab.MuRata.Type;

namespace SmartLab.MuRata.Indication
{
    public class SSIDRecordIndication : Payload
    {
        public SSIDRecordIndication(Payload payload)
            : base(payload) 
        { }

        public int GetNumberofRecords() { return this.GetData()[2]; }

        public WIFINetworkDetail[] GetRecords() 
        {
            int count = this.GetNumberofRecords();

            if (count <= 0)
                return null;

            int index = 0;
            int _position = 3;

            WIFINetworkDetail[] list = new WIFINetworkDetail[count]; ;

            byte[] value = this.GetData();

            while (_position < this.GetPosition()) 
            {
                list[index] = new WIFINetworkDetail();

                list[index].SetChannel(value[_position++]).SetRSSI(value[_position++]).SetSecurityMode((SecurityMode)value[_position++])
                    .SetBSSID(new byte[] { value[_position++], value[_position++], value[_position++], value[_position++], value[_position++], value[_position++] })
                    .SetNetworkType((BSSType)value[_position++]).SetMaxDataRate(value[_position++]);

                _position++;

                int start = _position;
                while (value[_position++] != 0x00) { }

                byte[] _string = new byte[_position - start - 1];
                Array.Copy(value, start, _string, 0, _string.Length);

                list[index].SetSSID(UTF8Encoding.UTF8.GetString(_string));
                
                index++;
            }

            return list;
        }
    }
}
