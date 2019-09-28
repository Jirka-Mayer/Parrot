namespace Parrot
{
    /// <summary>
    /// Options for parrot server
    /// </summary>
    public class ServerOptions
    {
        /// <summary>
        /// Use multiple threads to accept clients
        /// If false, single thread is used and requests are synchronized
        ///
        /// But a background listening thread will be used always.
        /// This option only defines whether to start new thread for a new client
        /// </summary>
        public bool multiThreading = true;
    }
}