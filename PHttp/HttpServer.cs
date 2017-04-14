//#region Original Code
//using System;
//using System.Web;
//using System.Collections.Generic;
//using System.Linq;
//using System.Net;
//using System.Net.Http;
//using System.Text;
//using System.Threading.Tasks;
//using System.Net.Sockets;
//using System.Threading;
//using System.Diagnostics;

//namespace PHttp
//{
//    public class HttpServer : IDisposable
//    {
//        #region Public Properties
//        public IPEndPoint EndPoint { get; private set; }
//        public int Port { get; private set; }
//        public int ReadBufferSize;
//        public int WriteBufferSize;
//        public string ServerBanner;
//        public TimeSpan ReadTimeout;
//        public TimeSpan WriteTimeout;
//        public TimeSpan ShutdownTimeout;
//        public object _syncLock = new object();
//        private Dictionary<HttpClient, bool> _clients;
//        #endregion

//        #region Private Properties
//        private TcpListener _listener;
//        internal HttpTimeoutManager TimeoutManager { get; private set; }
//        internal HttpServerUtility ServerUtility { get; private set; }
//        private AutoResetEvent _clientsChangedEvent = new AutoResetEvent(false);

//        public event HttpRequestEventHandler RequestReceived;
//        public event HttpExceptionEventHandler UnhandledException;

//        private HttpServerState _state = HttpServerState.Stopped;
//        private EventHandler StateChanged;
//        public HttpServerState State
//        {
//            get
//            {
//                return _state;
//            }
//            set
//            {
//                if (value != _state)
//                {
//                    _state = value; OnStateChanged(EventArgs.Empty);
//                }
//            }
//        }
//        #endregion

//        #region IDisposable Support
//        private bool _disposed = false; // To detect redundant calls

//        protected virtual void Dispose(bool disposing)
//        {
//            if (!_disposed)
//            {
//                if (_state == HttpServerState.Started)
//                {
//                    Stop();
//                }
//                if (_clientsChangedEvent != null)
//                {
//                    _clientsChangedEvent.Dispose();
//                    _clientsChangedEvent = null;
//                }

//                if (TimeoutManager != null)
//                {
//                    TimeoutManager.Dispose();
//                    TimeoutManager = null;
//                }
//                _disposed = true;
//            }
//        }

//        // TODO: override a finalizer only if Dispose(bool disposing) above has code to free unmanaged resources.
//        // ~HttpServer() {
//        //   // Do not change this code. Put cleanup code in Dispose(bool disposing) above.
//        //   Dispose(false);
//        // }

//        // This code added to correctly implement the disposable pattern.
//        public void Dispose()
//        {
//            // Do not change this code. Put cleanup code in Dispose(bool disposing) above.
//            Dispose(true);
//            // TODO: uncomment the following line if the finalizer is overridden above.
//            // GC.SuppressFinalize(this);
//        }
//        #endregion

//        #region Constructor
//        public HttpServer() : this(0)
//        {
//        }
//        public HttpServer(int port)
//        {
//            Port = port;
//            //EndPoint = new IPEndPoint(IPAddress.Loopback, Port);
//            EndPoint = new IPEndPoint(IPAddress.Loopback, Port);
//            ReadBufferSize = 4096;
//            WriteBufferSize = 4096;
//            ShutdownTimeout = TimeSpan.FromSeconds(30);
//            ReadTimeout = TimeSpan.FromSeconds(90);
//            WriteTimeout = TimeSpan.FromSeconds(90);
//            ServerBanner = String.Format("PHttp/{0}", GetType().Assembly.GetName().Version);
//            _clients = new Dictionary<HttpClient, bool>();
//            TimeoutManager = new HttpTimeoutManager(this);
//        }
//        #endregion

//        #region Methods
//        public void Start()
//        {
//            if (_state == HttpServerState.Stopped)
//            {
//                _state = HttpServerState.Starting;
//                Console.WriteLine("Server is " + _state + " at " + EndPoint + ".");

//                HttpTimeoutManager Timeout = TimeoutManager;
//                TcpListener listener = new TcpListener(EndPoint);
//                try
//                {
//                    if (listener != null)
//                    {
//                        listener.Start();
//                        EndPoint = (IPEndPoint)listener.LocalEndpoint;
//                        _listener = listener;
//                        HttpServerUtility server_util = ServerUtility;
//                        _state = HttpServerState.Started;
//                        Console.WriteLine("Server is " + _state + " at " + EndPoint + ".");
//                        BeginAcceptTcpClient();
//                    }
//                }
//                catch (PHttpException)
//                {
//                    throw new PHttpException("Failed to start HTTP server");
//                }
//                catch (Exception e)
//                {
//                    _state = HttpServerState.Stopped;
//                    throw new PHttpException("Server failed to start!", e);
//                }
//            }
//        }
//        public void Stop()
//        {
//            VerifyState(HttpServerState.Started);
//            try
//            {
//                TcpListener listener = _listener;
//                try
//                {
//                    if (listener != null)
//                    {
//                        _state = HttpServerState.Stopping;
//                        listener.Stop();
//                        StopClients();
//                    }
//                }
//                catch (Exception e)
//                {
//                    Console.WriteLine(e);
//                }
//            }
//            catch (PHttpException)
//            {
//                _state = HttpServerState.Stopped;
//                throw new PHttpException("Failed to stop HTTP server");
//            }
//        }
//        private void VerifyState(HttpServerState state)
//        {
//            if (_disposed == true)
//            {
//                throw new ObjectDisposedException(this.GetType().Name);
//            }
//            else if (_state != state)
//            {
//                throw new InvalidOperationException("Expected server to be in the " + state + " state.");
//            }
//        }
//        private void BeginAcceptTcpClient()
//        {
//            TcpListener listener = _listener;
//            if (listener != null)
//            {
//                listener.BeginAcceptTcpClient(AcceptTcpClientCallback, null);
//            }
//        }
//        private void AcceptTcpClientCallback(IAsyncResult asyncResult)
//        {
//            TcpListener listener = _listener;
//            if (listener == null)
//            {
//                return;
//            }
//            try
//            {
//                var tcpClient = listener.EndAcceptTcpClient(asyncResult);
//                if (_state == HttpServerState.Stopped)
//                {
//                    tcpClient.Close();
//                }
//                HttpClient newClient = new HttpClient(this, tcpClient);
//                RegisterClient(newClient);
//                newClient.BeginRequest();
//                BeginAcceptTcpClient();
//            }
//            catch (ObjectDisposedException objDispEx)
//            {
//                Console.WriteLine(objDispEx);
//            }
//            catch (Exception e)
//            {
//                Console.WriteLine(e);
//            }
//        }
//        private void RegisterClient(HttpClient client)
//        {
//            if (client == null)
//            {
//                throw new ArgumentNullException();
//            }
//            lock (_syncLock)
//            {
//                _clients.Add(client, true);
//                _clientsChangedEvent.Set();
//            }
//        }
//        internal void UnregisterClient(HttpClient client)
//        {
//            if (client == null)
//            {
//                throw new ArgumentNullException();
//            }
//            try
//            {
//                lock (_syncLock)
//                {
//                    Debug.Assert(_clients.ContainsKey(client));
//                    _clients.Remove(client);
//                    _clientsChangedEvent.Set();
//                }
//            }
//            catch (Exception e)
//            {
//                Console.WriteLine(e);
//            }
//        }
//        internal void RaiseRequest(HttpContext context)
//        {
//            if (context == null)
//                throw new ArgumentNullException("context");
//            OnRequestReceived(new HttpRequestEventArgs(context));
//        }

//        protected virtual void OnRequestReceived(HttpRequestEventArgs e)
//        {
//            var ev = RequestReceived;
//            if (ev != null)
//                ev(this, e);
//        }

//        internal bool RaiseUnhandledException(HttpContext context, Exception exception)
//        {
//            if (context == null)
//                throw new ArgumentNullException("context");
//            var e = new HttpExceptionEventArgs(context, exception);
//            OnUnhandledException(e);
//            return e.Handled;
//        }

//        protected virtual void OnUnhandledException(HttpExceptionEventArgs e)
//        {
//            var ev = UnhandledException;
//            if (ev != null)
//                ev(this, e);
//        }

//        private void StopClients()
//        {
//            var shutdownStarted = DateTime.Now;
//            bool forceShutdown = false;
//            // Clients that are waiting for new requests are closed.

//            List<HttpClient> clients;
//            lock (_syncLock)
//            {
//                clients = new List<HttpClient>(_clients.Keys);
//            }

//            foreach (var client in clients)
//            {
//                client.RequestClose();
//            }

//            // First give all clients a chance to complete their running requests.
//            while (true)
//            {
//                lock (_syncLock)
//                {
//                    if (_clients.Count == 0)
//                        break;
//                }

//                var shutdownRunning = DateTime.Now - shutdownStarted;

//                if (shutdownRunning >= ShutdownTimeout)
//                {
//                    forceShutdown = true;
//                    break;
//                }
//                _clientsChangedEvent.WaitOne(ShutdownTimeout - shutdownRunning);
//            }

//            if (!forceShutdown)
//                return;

//            // If there are still clients running after the timeout, their
//            // connections will be forcibly closed.
//            lock (_syncLock)
//            {
//                clients = new List<HttpClient>(_clients.Keys);
//            }

//            foreach (var client in clients)
//            {
//                client.ForceClose();
//            }

//            // Wait for the registered clients to be cleared.
//            while (true)
//            {
//                lock (_syncLock)
//                {
//                    if (_clients.Count == 0)
//                        break;
//                }
//                _clientsChangedEvent.WaitOne();
//            }
//        }
//        protected virtual void OnStateChanged(EventArgs e)
//        {
//            var ev = StateChanged;
//            if (ev != null)
//                ev(this, e);
//        }
//        #endregion
//    }
//}
//#endregion
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using static PHttp.Startup;

namespace PHttp
{
    public class HttpStateChangedEventArgs : EventArgs
    {
        public HttpStateChangedEventArgs(HttpServerState previousState, HttpServerState currentState)
        {
            PreviousState = previousState;
            CurrentState = currentState;
        }

        public HttpServerState CurrentState { get; private set; }
        public HttpServerState PreviousState { get; private set; }
    }

    public class HttpServer : IDisposable
    {
        private bool _disposed;
        private TcpListener _listener;
        private readonly object _syncLock = new object();
        private readonly Dictionary<HttpClient, bool> _clients = new Dictionary<HttpClient, bool>();
        private HttpServerState _state = HttpServerState.Stopped;
        private AutoResetEvent _clientsChangedEvent = new AutoResetEvent(false);

        public HttpServerState State
        {
            get { return _state; }
            private set
            {
                if (_state != value)
                {
                    var e = new HttpStateChangedEventArgs(_state, value);
                    _state = value;
                    OnStateChanged(e);
                }
            }
        }

        public event HttpRequestEventHandler RequestReceived;

        protected virtual void OnRequestReceived(HttpRequestEventArgs e)
        {
            var ev = RequestReceived;

            if (ev != null)
                ev(this, e);
        }

        public event HttpExceptionEventHandler UnhandledException;

        protected virtual void OnUnhandledException(HttpExceptionEventArgs e)
        {
            var ev = UnhandledException;

            if (ev != null)
                ev(this, e);
        }

        public event EventHandler StateChanged;

        protected virtual void OnStateChanged(HttpStateChangedEventArgs e)
        {
            var ev = StateChanged;
            if (ev != null)
                ev(this, e);
        }

        public IPEndPoint EndPoint { get; set; }

        public int ReadBufferSize { get; set; }

        public int WriteBufferSize { get; set; }

        public string ServerBanner { get; set; }

        public TimeSpan ReadTimeout { get; set; }

        public TimeSpan WriteTimeout { get; set; }

        public TimeSpan ShutdownTimeout { get; set; }

        internal HttpServerUtility ServerUtility { get; private set; }

        internal HttpTimeoutManager TimeoutManager { get; private set; }

        public HttpServer(int port)
        {
            EndPoint = new IPEndPoint(IPAddress.Loopback, port);

            ReadBufferSize = 4096;
            WriteBufferSize = 4096;
            ShutdownTimeout = TimeSpan.FromSeconds(30);
            ReadTimeout = TimeSpan.FromSeconds(90);
            WriteTimeout = TimeSpan.FromSeconds(90);

            ServerBanner = String.Format("PHttp/{0}", GetType().Assembly.GetName().Version);
        }

        public void Start()
        {
            VerifyState(HttpServerState.Stopped);

            State = HttpServerState.Starting;

            TimeoutManager = new HttpTimeoutManager(this);

            // Start the listener.

            var listener = new TcpListener(EndPoint);

            try
            {
                listener.Start();

                EndPoint = (IPEndPoint)listener.LocalEndpoint;

                _listener = listener;

                ServerUtility = new HttpServerUtility();

                Console.WriteLine("HTTP server running at {0}", EndPoint);
            }
            catch (Exception ex)
            {
                State = HttpServerState.Stopped;

                Console.WriteLine("Failed to start HTTP server", ex);

                throw new PHttpException("Failed to start HTTP server", ex);
            }

            State = HttpServerState.Started;

            BeginAcceptTcpClient();
        }

        public void Stop()
        {
            VerifyState(HttpServerState.Started);

            Console.WriteLine("Stopping HTTP server");

            State = HttpServerState.Stopping;

            try
            {
                // Prevent any new connections.

                _listener.Stop();

                // Wait for all clients to complete.

                StopClients();
            }
            catch (Exception ex)
            {
                Console.WriteLine("Failed to stop HTTP server", ex);

                throw new PHttpException("Failed to stop HTTP server", ex);
            }
            finally
            {
                _listener = null;

                State = HttpServerState.Stopped;

                Console.WriteLine("Stopped HTTP server");
            }
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

        private void BeginAcceptTcpClient()
        {
            var listener = _listener;
            if (listener != null)
                listener.BeginAcceptTcpClient(AcceptTcpClientCallback, null);
        }

        private void AcceptTcpClientCallback(IAsyncResult asyncResult)
        {
            try
            {
                var listener = _listener; // Prevent race condition.

                if (listener == null)
                    return;

                var tcpClient = listener.EndAcceptTcpClient(asyncResult);

                // If we've stopped already, close the TCP client now.

                if (_state != HttpServerState.Started)
                {
                    tcpClient.Close();
                    return;
                }

                var client = new HttpClient(this, tcpClient);

                RegisterClient(client);

                client.BeginRequest();

                BeginAcceptTcpClient();
            }
            catch (ObjectDisposedException)
            {
                // EndAcceptTcpClient will throw a ObjectDisposedException
                // when we're shutting down. This can safely be ignored.
            }
            catch (Exception ex)
            {
                Console.WriteLine("Failed to accept TCP client", ex);
            }
        }

        private void RegisterClient(HttpClient client)
        {
            if (client == null)
                throw new ArgumentNullException("client");

            lock (_syncLock)
            {
                _clients.Add(client, true);

                _clientsChangedEvent.Set();
            }
        }

        internal void UnregisterClient(HttpClient client)
        {
            if (client == null)
                throw new ArgumentNullException("client");

            lock (_syncLock)
            {
                Debug.Assert(_clients.ContainsKey(client));

                _clients.Remove(client);

                _clientsChangedEvent.Set();
            }
        }

        private void VerifyState(HttpServerState state)
        {
            if (_disposed)
                throw new ObjectDisposedException(GetType().Name);
            if (_state != state)
                throw new InvalidOperationException(String.Format("Expected server to be in the '{0}' state", state));
        }

        public void Dispose()
        {
            if (!_disposed)
            {
                if (_state == HttpServerState.Started)
                    Stop();

                if (_clientsChangedEvent != null)
                {
                    ((IDisposable)_clientsChangedEvent).Dispose();
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

        internal void RaiseRequest(HttpContext context)
        {
            if (context == null)
                throw new ArgumentNullException("context");

            OnRequestReceived(new HttpRequestEventArgs(context));
        }

        internal bool RaiseUnhandledException(HttpContext context, Exception exception)
        {
            if (context == null)
                throw new ArgumentNullException("context");

            var e = new HttpExceptionEventArgs(context, exception);

            OnUnhandledException(e);

            return e.Handled;
        }

        public bool ProcessRequest(HttpRequestEventArgs e, LoadDLLs methods)
        {
            ErrorHandler errorHandler = new ErrorHandler();
            List<string> IgnorePathList = new List<string>();
            string errorDetails = "";
            string defaultPath = "App1/Home/Index";
            string defaultURL = e.Request.Url.Scheme + "://" + e.Request.Url.Host.ToString()
                        + ":" + e.Request.Url.Port.ToString() + "/";

            IgnorePathList.Add("/css/mainStyle.css");
            IgnorePathList.Add("/favicon.ico");

            MimeTypes mimeTypes = new MimeTypes();

            string replacePath = ConfigurationManager.AppSettings["ReplacePath"]; ;
            string userprofile = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
            string path = e.Request.Url.PathAndQuery;
            HttpResponse res = e.Response;
            foreach (var p in IgnorePathList)
            {
                if (p == path) { return false; }
            }
            if (path == "" || path == "/")
            {
                path = defaultPath;
                e.Response.Redirect(e.Request.Url.ToString() + path);
                return false;
            }
            if (path[path.Length - 1] != '/') path = path + "/";
            string appName = path.Split('?')[0].Split('/')[1];
            string resources = ConfigurationManager.AppSettings["Virtual"];
            resources = resources.Replace(replacePath, userprofile);
            DirectoryInfo info = new DirectoryInfo(resources);
            if (!info.Exists) { return false; } //make sure directory exists
            string filePath = resources + appName;
            Console.WriteLine("\tFull Path = " + e.Request.Url);
            Console.WriteLine("\tPath = " + path);
            Console.WriteLine("\tApp Name = " + appName);

            foreach (var el in methods.Applications)
            {
                if (el.Name.ToUpper() == appName.Replace("/", "").ToUpper())
                {
                    Console.WriteLine("\n\tExecuting " + el + "...\n");
                    if (path.Split('?')[0].Split('/').Length >= 3)
                    {
                        foreach (var ctrl in methods.Controllers)
                        {
                            var controllerName = ctrl.ToString().Replace("Mvc.Controllers.", "");
                            controllerName = controllerName.Replace("Controller", "");
                            if (path.Split('?')[0].Split('/')[2].ToUpper() == controllerName.ToUpper())
                            {
                                el.ExecuteAction(methods, e);
                                return true;
                            }
                        }
                    }
                    defaultURL = defaultURL + "404";
                    errorHandler.RenderErrorPage(404, e);
                    e.Response.Redirect(defaultURL);
                    return false;
                }
            }
            if (File.Exists(filePath) == true)
            {
                using (var stream = File.Open(filePath, FileMode.Open))
                {
                    res.ContentType = mimeTypes.GetMimeType(Path.GetExtension(filePath));
                    byte[] buffer = new byte[4096];
                    int read;
                    while ((read = stream.Read(buffer, 0, buffer.Length)) != 0)
                    {
                        res.OutputStream.Write(buffer, 0, read);
                    }
                    return true;
                }
            }
            errorHandler.RenderErrorPage(404, e);
            return false;
        }
    }
}