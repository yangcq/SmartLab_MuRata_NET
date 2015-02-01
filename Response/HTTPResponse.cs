using System;
using System.Text;

namespace SmartLab.MuRata.Response
{
    public class HTTPResponse: Payload
    {
        private int payloadOffset;

        private int contentLength;
        private int statusCode;
        private string contentType;

        public HTTPResponse(Payload payload)
            : base(payload)
        {
            statusCode = this.GetData()[2] << 8 | this.GetData()[3];
            if (statusCode >= 100)
            {
                contentLength = (this.GetData()[4] & 0x7F) << 8 | this.GetData()[5];

                int _position = 6;
                int start = 6;

                while (this.GetData()[_position++] != 0x00) { }
                payloadOffset = _position;

                byte[] _string = new byte[_position - start - 1];
                Array.Copy(this.GetData(), start, _string, 0, _string.Length);
                this.contentType = UTF8Encoding.UTF8.GetString(_string);
            }
        }

        /// <summary>
        /// Present only if Status code is HTTP status code.
        /// </summary>
        /// <returns></returns>
        public bool isMoreDataComing() { return (this.GetData()[4] >> 7) == 0x01 ? true : false; }

        /// <summary>
        /// Present only if Status code is HTTP status code.
        /// </summary>
        /// <returns></returns>
        public int GetContentLength() { return this.contentLength; }

        /// <summary>
        /// Status code can be either SNIC status code (which is less than 100) listed in Table 18, or HTTP status code defined in HTTP spec 1.1 (which is bigger than 100).
        /// </summary>
        /// <returns></returns>
        public int GetStatusCode() { return this.statusCode; }

        /// <summary>
        /// Present only if Status code is HTTP status code.
        /// </summary>
        /// <param name="index"></param>
        /// <returns></returns>
        public byte GetContent(int index) { return this.GetData()[index + payloadOffset]; }

        /// <summary>
        /// Present only if Status code is HTTP status code.
        /// </summary>
        /// <param name="index"></param>
        /// <returns></returns>
        public byte[] GetContent()
        {
            byte[] con = new byte[this.contentLength];
            Array.Copy(this.GetData(), payloadOffset, con, 0, this.contentLength);
            return con;
        }

        /// <summary>
        /// Present only if Status code is HTTP status code.
        /// </summary>
        /// <returns></returns>
        public string GetContentType() { return this.contentType; }

        /// <summary>
        /// Present only if Status code is HTTP status code.
        /// </summary>
        /// <returns></returns>
        public int GetContentOffset() { return this.payloadOffset; }
    }
}
