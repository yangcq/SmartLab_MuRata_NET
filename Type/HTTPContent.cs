using System.Collections;
using System.Text;

namespace SmartLab.MuRata.Type
{
    public class HTTPContent
    {
        private static readonly string HEADER_SEPARATE = ": ";
        private static readonly string HEADER_TEMINATE = "\r\n";

        private HTTPMethod method;
        private byte timeout;
        private int remotePort;
        private string remoteHost;
        private string uri;

        private int contentLength = 0;
        private string contentType;

        private Hashtable otherHeaders = new Hashtable();

        private byte[] body;

        /// <summary>
        /// URI, Content Type, and Other header are all NUL terminated char strings. URI must be specified, e.g., “index.htm”. If Content Type is an empty string, the default will be “text/plain”. If it is a GET,
        /// </summary>
        /// <param name="method"></param>
        /// <param name="remoteHost"></param>
        /// <param name="remotePort"></param>
        /// <param name="uri"></param>
        /// <param name="timeout"></param>
        /// <param name="contentType"></param>
        public HTTPContent(HTTPMethod method, string remoteHost, int remotePort, string uri, byte timeout, string contentType = null)
        {
            this.SetMethod(method).SetRemoteHost(remoteHost).SetRemotePort(remotePort).SetURI(uri).SetTimeout(timeout).SetContentType(contentType);
        }

        public HTTPMethod GetMethod() { return this.method; }

        public HTTPContent SetMethod(HTTPMethod method)
        {
            this.method = method;
            return this;
        }

        public byte GetTimeout() { return this.timeout; }

        /// <summary>
        /// Timeout is in seconds. If Timeout is 0, it means wait forever. A complete HTTP request will block other commands until either a response is received or timeout. So timeout value of 0 is not recommended. If timeout happens, the response status code would be SNIC_TIMEOUT. If it is chunked encoding in the response, the last chunk should be received before Timeout; otherwise, the connection is closed. If the HTTP request has more data to send (POST), it is not considered a complete HTTP request, and other commands are not blocked until the series of SNIC_HTTP_MORE_REQ are finished.
        /// </summary>
        /// <param name="timeout"></param>
        /// <returns></returns>
        public HTTPContent SetTimeout(byte timeout)
        {
            this.timeout = timeout;
            return this;
        }

        public int GetRemotePort() { return this.remotePort; }

        public HTTPContent SetRemotePort(int port)
        {
            this.remotePort = port;
            return this;
        }

        public string GetRemoteHost() { return this.remoteHost; }

        public HTTPContent SetRemoteHost(string host)
        {
            this.remoteHost = host;
            return this;
        }

        public string GetURI() { return this.uri; }

        public HTTPContent SetURI(string uri)
        {
            this.uri = uri;
            return this;
        }

        public string GetContentType() { return this.contentType; }

        public HTTPContent SetContentType(string contentType)
        {
            if (contentType == null)
                this.contentType = "";
            else
                this.contentType = contentType;
            return this;
        }

        public string GetOtherHeader(object key) { return (string)this.otherHeaders[key]; }

        public HTTPContent SetOtherHeader(object key, object value)
        {
            this.otherHeaders[key] = value;
            return this;
        }

        public string GetAllOtherHeaders()
        {
            StringBuilder sb = new StringBuilder();
            foreach (DictionaryEntry entry in this.otherHeaders)
            {
                sb.Append(entry.Key);
                sb.Append(HEADER_SEPARATE);
                sb.Append(entry.Value);
                sb.Append(HEADER_TEMINATE);
            }
            return sb.ToString();
        }

        public byte[] GetBody() { return this.body; }

        public HTTPContent SetBody(string body) 
        {
            if (body == null)
            {
                this.contentLength = 0;
                this.body = null;
                return this;
            }

            return SetBody(UTF8Encoding.UTF8.GetBytes(body)); 
        }

        public HTTPContent SetBody(byte[] body)
        {
            if (body == null)
                this.contentLength = 0;
            else
                this.contentLength = body.Length;

            this.body = body;
            return this;
        }

        public HTTPContent ClearBody() 
        {
            this.contentLength = 0;
            this.body = null;
            return this;
        }

        public int GetContentLength() { return this.contentLength; }

    }
}