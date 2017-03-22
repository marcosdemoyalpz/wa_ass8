using System;
using System.Web;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using System.Net.Sockets;
using System.Threading;

namespace PHttp
{
    class HttpServer : IDisposable
    {
        #region Public Properties
        public IPEndPoint EndPoint { get; private set; }
        public int Port { get; private set; }
        public int ReadBufferSize;
        public int WriteBufferSize;
        public string ServerBanner;
        public TimeSpan ReadTimeout;
        public TimeSpan WriteTimeout;
        public TimeSpan ShutdownTimeout;
        public object _synclock;
        public Dictionary<HttpClient, bool> _clients;
        #endregion

        #region Private Properties        
        private TcpListener _listener;
        internal HttpTimeoutManager TimeoutManager { get; private set; }
        internal HttpServerUtility ServerUtility { get; private set; }
        private AutoResetEvent _clientsChangedEvent = new AutoResetEvent(false);

        private HttpServerState _state = HttpServerState.Stopped;
        private EventHandler StateChanged;
        public HttpServerState State
        {
            get
            {
                return _state;
            }
            set
            {
                if (value != _state)
                {
                    _state = value; OnStateChanged(EventArgs.Empty);
                }
            }
        }
        #endregion

        #region IDisposable Support
        private bool _disposed = false; // To detect redundant calls

        protected virtual void Dispose(bool disposing)
        {
            if (!_disposed)
            {
                if (_state == HttpServerState.Started)
                {
                    Stop();
                }
                if (_clientsChangedEvent != null)
                {
                    _clientsChangedEvent.Dispose();
                    _clientsChangedEvent = null;
                }

                if (TimeoutManager != null)
                {
                    TimeoutManager.Dispose();
                    TimeoutManager = null;
                }
                _disposed = true;
            }
        }

        // TODO: override a finalizer only if Dispose(bool disposing) above has code to free unmanaged resources.
        // ~HttpServer() {
        //   // Do not change this code. Put cleanup code in Dispose(bool disposing) above.
        //   Dispose(false);
        // }

        // This code added to correctly implement the disposable pattern.
        public void Dispose()
        {
            // Do not change this code. Put cleanup code in Dispose(bool disposing) above.
            Dispose(true);
            // TODO: uncomment the following line if the finalizer is overridden above.
            // GC.SuppressFinalize(this);
        }
        #endregion

        #region Constructor
        public HttpServer() : this(0)
        {
        }
        public HttpServer(int port)
        {
            Port = port;
            EndPoint = new IPEndPoint(IPAddress.Loopback, Port);
            ReadBufferSize = 4096;
            WriteBufferSize = 4096;
            ShutdownTimeout = TimeSpan.FromSeconds(30);
            ReadTimeout = TimeSpan.FromSeconds(90);
            WriteTimeout = TimeSpan.FromSeconds(90);
            ServerBanner = String.Format("PHttp/{0}", GetType().Assembly.GetName().Version);
        }
        #endregion

        #region Methods
        public void Start()
        {
            if (_state == HttpServerState.Stopped)
            {
                _state = HttpServerState.Starting;
                Console.WriteLine("Server is starting at " + EndPoint + ".");

                HttpTimeoutManager Timeout = TimeoutManager;
                TcpListener listener = new TcpListener(EndPoint);
                try
                {
                    listener.Start();
                    EndPoint = (IPEndPoint)listener.LocalEndpoint;
                    listener = _listener;
                    HttpServerUtility server_util = ServerUtility;
                    Console.WriteLine("Server is running at " + EndPoint + ".");
                    _state = HttpServerState.Started;
                    BeginAcceptTcpClient();
                }
                catch (Exception e)
                {
                    _state = HttpServerState.Stopped;
                    throw new PHttpException("Server failed to start!", e);
                }
            }
        }
        public void Stop() { throw new NotImplementedException(); }
        private void VerifyState(HttpServerState state)
        {
            if (_disposed == true)
            {
                throw new ObjectDisposedException(this.GetType().Name);
            }
            else if (_state != state)
            {
                throw new InvalidOperationException("Expected server to be in the " + state + " state.");
            }
        }
        private void StopClients() { throw new NotImplementedException(); }
        private void BeginAcceptTcpClient()
        {
            TcpListener listener = _listener;
            if (listener != null)
            {
                listener.BeginAcceptTcpClient(AcceptTcpClientCallback, null);

            }
        }
        private void AcceptTcpClientCallback(IAsyncResult asyncResult) { throw new NotImplementedException(); }
        private void RegisterClient(HttpClient client)
        {
            if (client == null)
            {
                throw new ArgumentException();
            }
            lock (_synclock)
            {
                _clients.Add(client, true);
                _clientsChangedEvent.Set();
            }
        }
        internal void UnregisterClient(HttpClient client) { throw new NotImplementedException(); }
        protected virtual void OnStateChanged(EventArgs args)
        {
            StateChanged.Invoke(this, args);
        }
        #endregion
    }
}
