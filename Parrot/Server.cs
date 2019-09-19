using System;
using System.Net;
using System.Net.Sockets;
using System.Threading;

namespace Parrot
{
    /// <summary>
    /// Listens on a given port and accepts incoming connections
    /// </summary>
    public class Server
    {
        /// <summary>
        /// Lock for synchronizing operations on this instance
        /// </summary>
        private readonly object syncLock = new object();

        /// <summary>
        /// Underlying listener instance
        /// </summary>
        private TcpListener listener;

        /// <summary>
        /// Thread on which the accepting loop runs
        /// </summary>
        private Thread listeningThread;

        /// <summary>
        /// IP address on which we listen
        /// </summary>
        private readonly string ipAddress;
        
        /// <summary>
        /// Port on which we listen
        /// </summary>
        private readonly int port;

        /// <summary>
        /// What method to call to handle a new client.
        /// Has to be readonly. Is accessed by multiple threads.
        /// </summary>
        private readonly Action<Client> clientHandler;

        // flags indicating server state
        // (server can only be started and stopped once)
        private bool started;
        private bool stopped;

        // is the server being stopped
        // WARNING: only access with the stopping lock acquired
        private bool stopping;
        private readonly object stoppingLock = new object();

        /// <summary>
        /// Create new server instance with proper configuration
        /// </summary>
        /// <param name="ipAddress"></param>
        /// <param name="port"></param>
        /// <param name="clientHandler"></param>
        public Server(string ipAddress, int port, Action<Client> clientHandler)
        {
            this.ipAddress = ipAddress;
            this.port = port;
            this.clientHandler = clientHandler;
        }

        /// <summary>
        /// Starts listening for incoming connections
        /// This method does not block, the listening is performed on another thread
        /// </summary>
        public void Start()
        {
            lock (syncLock)
            {
                if (started)
                    throw new InvalidOperationException(
                        "Cannot start the server twice."
                    );

                // TCP listener
                listener = new TcpListener(IPAddress.Parse(ipAddress), port);
                listener.Start();

                // listening thread
                listeningThread = new Thread(AcceptingLoop);
                listeningThread.Start();

                // flag
                started = true;
            }
        }

        /// <summary>
        /// The loop for accepting clients
        /// Runs in it's own thread
        /// </summary>
        private void AcceptingLoop()
        {
            try
            {
                while (true)
                {
                    Socket socket;
                    
                    try
                    {
                        // Blocks until someone connects.
                        socket = listener.AcceptSocket();
                        
                        // send message immediately to reduce latency
                        socket.NoDelay = true;
                    }
                    catch
                    {
                        // If we are stopping, then this exception
                        // is probably ok. Otherwise keep it bubbling up.
                        lock (stoppingLock)
                            if (stopping)
                                break; // break the while(true) loop

                        throw;
                    }

                    // start handler in a new thread
                    new Thread(() => {
                        using (var client = new Client(socket))
                            RunClientHandler(client);
                    }).Start();
                }
            }
            catch (Exception e)
            {
                // log any unhandled exceptions so that we know about them
                Console.WriteLine(
                    "Exception occured inside Parrot server " +
                    "client accepting thread:"
                );
                Console.WriteLine(e);
            }
        }

        /// <summary>
        /// Executes the client handler with proper exception handling.
        /// This method already runs in a separate thread.
        /// </summary>
        private void RunClientHandler(Client client)
        {
            try
            {
                clientHandler.Invoke(client);
            }
            catch (Exception e)
            {
                // log any unhandled exceptions so that we know about them
                Console.WriteLine(
                    "Exception occured inside Parrot server " +
                    "client handler method:"
                );
                Console.WriteLine(e);
            }
        }

        /// <summary>
        /// Stops the server
        /// This method is thread-safe
        /// </summary>
        public void Stop()
        {
            lock (syncLock)
            {
                if (!started)
                    return;

                if (stopped)
                    return;

                lock (stoppingLock)
                    stopping = true;
                
                // TPC listener
                listener.Stop();
                listener = null;
                
                // listening thread
                // NOTE: no need to abort, see the AcceptingLoop
                listeningThread.Join(); // wait for the thread to stop
                listeningThread = null;

                // flag
                stopped = true;
                
                lock (stoppingLock)
                    stopping = false;
            }
        }
    }
}