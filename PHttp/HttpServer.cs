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
using System.Diagnostics;

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
        public object _syncLock;
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
                catch (PHttpException)
                {
                    throw new PHttpException("Failed to start HTTP server");
                }
                catch (Exception e)
                {
                    _state = HttpServerState.Stopped;
                    throw new PHttpException("Server failed to start!", e);
                }
            }
        }
        public void Stop()
        {
            VerifyState(HttpServerState.Started);
            try
            {
                _state = HttpServerState.Stopping;
                _listener.Stop();
                StopClients();
            }
            catch (PHttpException)
            {
                throw new PHttpException("Failed to stop HTTP server");
            }
        }
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
        private void BeginAcceptTcpClient()
        {
            TcpListener listener = _listener;
            if (listener != null)
            {
                listener.BeginAcceptTcpClient(AcceptTcpClientCallback, null);

            }
        }
        private void AcceptTcpClientCallback(IAsyncResult asyncResult)
        {
            TcpListener listener = _listener;
            if (listener == null)
            {
                return;
            }
            try
            {
                var tcpClient = listener.EndAcceptTcpClient(asyncResult);
                if (_state == HttpServerState.Stopped)
                {
                    tcpClient.Close();
                }
                HttpClient newClient = new HttpClient();
                RegisterClient(newClient);
                newClient.BeginRequest();
                BeginAcceptTcpClient();
            }
            catch (ObjectDisposedException objDispEx)
            {
                Console.WriteLine(objDispEx);
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
            }
        }
        private void RegisterClient(HttpClient client)
        {
            if (client == null)
            {
                throw new ArgumentNullException();
            }
            lock (_syncLock)
            {
                _clients.Add(client, true);
                _clientsChangedEvent.Set();
            }
        }
        internal void UnregisterClient(HttpClient client)
        {
            if (client == null)
            {
                throw new ArgumentNullException();
            }
            try
            {
                lock (_syncLock)
                {
                    Debug.Assert(_clients.ContainsKey(client));
                    _clients.Remove(client);
                    _clientsChangedEvent.Set();
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
            }
        }
        internal void RaiseRequest(HttpContext context)
        {
            if (context == null)
                throw new ArgumentNullException("context");
            OnRequestReceived(new HttpRequestEventArgs(context));
        }

        private void OnRequestReceived(HttpRequestEventArgs httpRequestEventArgs)
        {
            throw new NotImplementedException();
        }

        internal bool RaiseUnhandledException(HttpContext context, Exception exception)
        {
            if (context == null)
                throw new ArgumentNullException("context");
            var e = new HttpExceptionEventArgs(context, exception);
            OnUnhandledException(e);
            return e.Handled;
        }

        private void OnUnhandledException(HttpExceptionEventArgs e)
        {
            throw new NotImplementedException();
        }

        private void StopClients()
        {
            var shutdownStarted = DateTime.Now;
            bool forceShutdown = false;
            // Clients that are waiting for new requests are closed.

            List<HttpClient> clients;
            lock (_syncLock)
            {
                clients = new List<HttpClient>(_clients.Keys);
            }

            foreach (var client in clients)
            {
                client.RequestClose();
            }

            // First give all clients a chance to complete their running requests.
            while (true)
            {
                lock (_syncLock)
                {
                    if (_clients.Count == 0)
                        break;
                }

                var shutdownRunning = DateTime.Now - shutdownStarted;

                if (shutdownRunning >= ShutdownTimeout)
                {
                    forceShutdown = true;
                    break;
                }
                _clientsChangedEvent.WaitOne(ShutdownTimeout - shutdownRunning);
            }

            if (!forceShutdown)
                return;

            // If there are still clients running after the timeout, their
            // connections will be forcibly closed.
            lock (_syncLock)
            {
                clients = new List<HttpClient>(_clients.Keys);
            }

            foreach (var client in clients)
            {
                client.ForceClose();
            }

            // Wait for the registered clients to be cleared.
            while (true)
            {
                lock (_syncLock)
                {
                    if (_clients.Count == 0)
                        break;
                }
                _clientsChangedEvent.WaitOne();
            }
        }
        protected virtual void OnStateChanged(EventArgs args)
        {
            StateChanged.Invoke(this, args);
        }
        #endregion
    }
}
