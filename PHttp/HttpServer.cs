using System;
using System.Web;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace PHttp
{
    class HttpServer : IDisposable
    {
        #region Public Properties
        public IPEndPoint EndPoint;
        public int ReadBufferSize;
        public int WriteBufferSize;
        public string ServerBanner;
        public TimeSpan ReadTimeout;
        public TimeSpan WriteTimeout;
        public TimeSpan ShutdownTimeout;
        #endregion

        #region Private Properties
        private HttpServerUtility ServerUtility;
        private HttpListenerTimeoutManager TimeoutManager;
        #endregion

        #region IDisposable Support
        private bool disposedValue = false; // To detect redundant calls

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    // TODO: dispose managed state (managed objects).
                }

                // TODO: free unmanaged resources (unmanaged objects) and override a finalizer below.
                // TODO: set large fields to null.

                disposedValue = true;
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
        public HttpServer()
        {
            EndPoint = new IPEndPoint(IPAddress.Loopback, 0);
            ReadBufferSize = 4096;
            WriteBufferSize = 4096;
            ShutdownTimeout = TimeSpan.FromSeconds(30);
            ReadTimeout = TimeSpan.FromSeconds(90);
            WriteTimeout = TimeSpan.FromSeconds(90);
            ServerBanner = String.Format("PHttp/{0}", GetType().Assembly.GetName().Version);
        }
        #endregion

        #region Methods
        public void Start() { throw new NotImplementedException(); }
        public void Stop() { throw new NotImplementedException(); }
        private void VerifyState(HttpServerState state) { throw new NotImplementedException(); }
        private void StopClients() { throw new NotImplementedException(); }
        private void BeginAcceptTcpClient() { throw new NotImplementedException(); }
        private void AcceptTcpClientCallback(IAsyncResult asyncResult) { throw new NotImplementedException(); }
        private void RegisterClient(HttpClient client) { throw new NotImplementedException(); }
        internal void UnregisterClient(HttpClient client) { throw new NotImplementedException(); }
        #endregion
    }
}
