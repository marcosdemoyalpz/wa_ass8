using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace PHttp
{
    internal class HttpClient : IDisposable
    {
        #region Members
        public enum ClientState
        {
            ReadingProlog,
            ReadingHeaders,
            ReadingContent,
            WritingHeaders,
            WritingContent,
            Closed
        }
        bool _disposed;
        ClientState _state;
        HttpContext _context;
        readonly byte[] _writeBuffer;
        NetworkStream _stream;
        MemoryStream _writeStream;
        bool _errored;
        HttpRequestParser _parser;
        private static readonly Regex PrologRegex = new Regex("^([A-Z]+) ([^ ]+) (HTTP/[^ ]+)$", RegexOptions.Compiled);
        #endregion
        #region Properties
        public HttpServer Server;
        /*readonly */
        public TcpClient TcpClient;
        /*readonly */
        public HttpReadBuffer ReadBuffer;
        public Stream InputStream;
        /*readonly */
        public Dictionary<string, string> Headers;
        /*readonly */
        public string Method;
        /*readonly */
        public string Protocol;
        /*readonly */
        public string Request;
        public List<HttpMultiPartItem> MultiPartItems;
        public NameValueCollection PostParameters;
        #endregion
        #region Constructor
        public HttpClient() : this(0)
        {
        }
        public HttpClient(int port)
        {
            ReadBuffer = new HttpReadBuffer(Server.ReadBufferSize);
            _writeBuffer = new byte[Server.WriteBufferSize];
            Method = null;
            Protocol = null;
            Request = null;
            Headers = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        }
        #endregion
        #region Methods
        private void ReadCallback(IAsyncResult syncResult)
        {
            if (_disposed == true)
            {
                //Exit();
            }
            else if (_state == ClientState.ReadingProlog && Server.State != HttpServerState.Started)
            {
                Dispose();
                //Exit();
            }
            try
            {
                ReadBuffer.EndRead(_stream, syncResult);
            }
            catch (ObjectDisposedException)
            {
                Dispose();
            }
            catch (Exception s)
            {
                ProcessException(s);
            }
            if (ReadBuffer.DataAvailable)
            {
                ProcessReadBuffer();
            }
            else
            {
                Dispose();
            }
        }
        private void ProcessException(Exception e) { }
        private void ProcessReadBuffer()
        {
            do
            {
                switch (_state)
                {
                    case ClientState.ReadingProlog:
                        ProcessProlog();
                        break;
                    case ClientState.ReadingHeaders:
                        ProcessHeaders();
                        break;
                    case ClientState.ReadingContent:
                        ProcessContent();
                        break;
                    default:
                        throw new InvalidOperationException("Invalid state!");
                }
            }
            while (ReadBuffer.DataAvailable && _writeStream == null);
            if (_writeStream == null)
            {
                BeginRead();
            }
        }
        private void ProcessProlog()
        {
            string readLine = "";
            readLine = ReadBuffer.ReadLine();
            if (readLine == "" || readLine == null)
            {
                //Exit();
            }
            var match = PrologRegex.Match(readLine, 0);
            if (match.Success == false)
            {
                throw new ProtocolException("The prolog '" + readLine + "' could not be parsed!");
            }
            Method = match.Groups[0].ToString();
            Request = match.Groups[1].ToString();
            Protocol = match.Groups[2].ToString();
            _state = ClientState.ReadingHeaders;
            ProcessHeaders();
        }
        private void ProcessHeaders()
        {
            string line = ReadBuffer.ReadLine();
            if (line != null)
            {
                if (line.Length == 0)
                {
                    ReadBuffer.Reset();
                    _state = ClientState.ReadingContent;
                    ProcessContent();
                    //Exit();
                }
                else
                {
                    string pattern = ":";
                    Regex rgx = new Regex(pattern);
                    var parts = rgx.Split(line, 2);
                    if (parts.Length != 2)
                    {
                        throw new ProtocolException("Received header without colon");
                    }
                    else
                    {
                        Headers[parts[0].Trim()] = parts[1].Trim();
                    }
                }
            }
        }
        private void ProcessContent()
        {
            if (_parser != null)
            {
                _parser.Parse();
                //Exit();
            }
            else if (ProcessExpectHeader() == true)
            {
                //Exit();
            }
            else if (ProcessContentLengthHeader() == true)
            {
                //Exit();
            }
            else
            {
                ExecuteRequest();
            }

        }
        public void ExecuteRequest() { }
        private void Reset()
        {
            _state = ClientState.ReadingProlog;
            _context = null;
            if (_parser != null)
            {
                _parser.Dispose();
                _parser = null;
            }

            if (_writeStream != null)
            {
                _writeStream.Dispose();
                _writeStream = null;
            }

            if (InputStream != null)
            {
                InputStream.Dispose();
                InputStream = null;
            }

            ReadBuffer.Reset();
            Method = null;
            Protocol = null;
            Request = null;
            Headers = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            PostParameters = new NameValueCollection();

            if (MultiPartItems != null)
            {
                foreach (var item in MultiPartItems)
                {
                    if (item.Stream != null)
                        item.Stream.Dispose();
                }

                MultiPartItems = null;
            }
        }
        public void Dispose()
        {
            if (!_disposed)
            {
                Server.UnregisterClient(this);
                _state = ClientState.Closed;
                if (_stream != null)
                {
                    _stream.Dispose();
                    _stream = null;
                }
                if (TcpClient != null)
                {
                    TcpClient.Close();
                    TcpClient = null;
                }
                Reset();
                _disposed = true;
            }
        }
        public void RequestClose()
        {
            if (_state == ClientState.ReadingProlog)
            {
                var stream = _stream;
                if (stream != null)
                    stream.Dispose();
            }
        }
        public void ForceClose()
        {
            var stream = _stream;
            if (stream != null)
                stream.Dispose();
        }
        public void UnsetParser()
        {
            Debug.Assert(_parser != null);
            _parser = null;
        }
        private bool ProcessExpectHeader()
        {
            // Process the Expect: 100-continue header.
            string expectHeader;
            if (Headers.TryGetValue("Expect", out expectHeader))
            {
                // Remove the expect header for the next run.
                Headers.Remove("Expect");
                int pos = expectHeader.IndexOf(';');
                if (pos != -1)
                    expectHeader = expectHeader.Substring(0, pos).Trim();
                if (!String.Equals("100-continue", expectHeader, StringComparison.OrdinalIgnoreCase))
                    throw new ProtocolException(String.Format("Could not process Expect header '{0}'", expectHeader));
                SendContinueResponse();
                return true;
            }
            return false;
        }
        private bool ProcessContentLengthHeader()
        {
            // Read the content.
            string contentLengthHeader;
            if (Headers.TryGetValue("Content-Length", out contentLengthHeader))
            {
                int contentLength;
                if (!int.TryParse(contentLengthHeader, out contentLength))
                    throw new ProtocolException(String.Format("Could not parse Content-Length header '{0}'", contentLengthHeader));
                string contentTypeHeader;
                string contentType = null;
                string contentTypeExtra = null;
                if (Headers.TryGetValue("Content-Type", out contentTypeHeader))
                {
                    string[] parts = contentTypeHeader.Split(new[] { ';' }, 2);
                    contentType = parts[0].Trim().ToLowerInvariant();
                    contentTypeExtra = parts.Length == 2 ? parts[1].Trim() : null;
                }
                if (_parser != null)
                {
                    _parser.Dispose();
                    _parser = null;
                }
                switch (contentType)
                {
                    case "application/x-www-form-urlencoded":
                        _parser = new HttpUrlEncodedRequestParser(this, contentLength);
                        break;
                    case "multipart/form-data":
                        string boundary = null;
                        if (contentTypeExtra != null)
                        {
                            string[] parts = contentTypeExtra.Split(new[] { '=' }, 2);
                            if (
                                parts.Length == 2 &&
                                String.Equals(parts[0], "boundary", StringComparison.OrdinalIgnoreCase)
                            )
                                boundary = parts[1];
                        }
                        if (boundary == null)
                            throw new ProtocolException("Expected boundary with multipart content type");
                        _parser = new HttpMultiPartRequestParser(this, contentLength, boundary);
                        break;
                    default:
                        _parser = new HttpUnknownRequestParser(this, contentLength);
                        break;
                }
                // We've made a parser available. Recurs back to start processing
                // with the parser.
                ProcessContent();
                return true;
            }
            return false;
        }
        private void SendContinueResponse()
        {
            var sb = new StringBuilder();
            sb.Append(Protocol);
            sb.Append(" 100 Continue\r\nServer: ");
            sb.Append(Server.ServerBanner);
            sb.Append("\r\nDate: ");
            sb.Append(DateTime.UtcNow.ToString("R"));
            sb.Append("\r\n\r\n");
            var bytes = Encoding.ASCII.GetBytes(sb.ToString());
            if (_writeStream != null)
                _writeStream.Dispose();
            _writeStream = new MemoryStream();
            _writeStream.Write(bytes, 0, bytes.Length);
            _writeStream.Position = 0;
            //BeginWrite();
        }
        private void BeginRead()
        {
            if (_disposed)
                return;
            try
            {
                // Reads should be within a certain timeframe

                Server.TimeoutManager.ReadQueue.Add(
                    ReadBuffer.BeginRead(_stream, ReadCallback, null),
                    this
                );
            }
            catch (Exception)
            {
                Dispose();
            }
        }
        public void BeginRequest()
        {
            Reset();
            BeginRead();
        }
        #endregion
    }
}
