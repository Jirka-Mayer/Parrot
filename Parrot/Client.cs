using System;
using System.Net;
using System.Net.Sockets;
using System.Text;

namespace Parrot
{
    /// <summary>
    /// Represents an established Parrot connection over which you
    /// can send messages
    /// </summary>
    public class Client : IDisposable
    {
        /// <summary>
        /// Underlying TCP socket
        /// </summary>
        private readonly Socket socket;

        /// <summary>
        /// Wrap Parrot client around an existing TPC socket
        /// </summary>
        public Client(Socket socket)
        {
            this.socket = socket;
        }

        /// <summary>
        /// Create a client and connect it to a given server
        /// </summary>
        public static Client Connect(string ipAddress, int port)
        {
            var socket = new Socket(
                AddressFamily.InterNetwork,
                SocketType.Stream,
                ProtocolType.Tcp
            );
            
            // send message immediately to reduce latency
            socket.NoDelay = true;
            
            socket.Connect(
                IPAddress.Parse(ipAddress),
                port
            );
            
            return new Client(socket);
        }

        /// <summary>
        /// Close the connection
        /// </summary>
        public void Disconnect()
        {
            socket.Close();
        }

        /// <summary>
        /// Close the connection
        /// </summary>
        public void Dispose() => Disconnect();
        
        /// <summary>
        /// Close the connection
        /// </summary>
        public void Close() => Disconnect();

        ///////////////////////////
        // Textual communication //
        ///////////////////////////

        /// <summary>
        /// Sends textual message to the other side
        /// </summary>
        /// <param name="messageType">Type of the message</param>
        /// <param name="payload">Payload</param>
        public void SendTextMessage(int messageType, string payload)
        {
            SendMessage(
                messageType,
                Encoding.UTF8.GetBytes(payload ?? "")
            );
        }

        /// <summary>
        /// Sends textual message to the other side
        /// </summary>
        /// <param name="messageType">Type of the message</param>
        /// <param name="payload">Payload</param>
        public void SendTextMessage(Enum messageType, string payload) =>
            SendTextMessage(Convert.ToInt32(messageType), payload);

        /// <summary>
        /// Receives a textual message from the other side
        /// </summary>
        /// <param name="messageType">Type of the message received</param>
        /// <returns>Content of the message</returns>
        public string ReceiveTextMessage(out int messageType)
        {
            return Encoding.UTF8.GetString(
                ReceiveMessage(out messageType)
            );
        }

        /// <summary>
        /// Receives textual message from the other side
        /// </summary>
        /// <param name="messageType">Type of the message as enum E</param>
        /// <typeparam name="E">Enum of message types</typeparam>
        /// <returns>Message content</returns>
        public string ReceiveTextMessage<E>(out E messageType) where E : IConvertible
        {
            if (!typeof(E).IsEnum)
                throw new ArgumentException("T must be an enumerated type.");

            string message = ReceiveTextMessage(out int type);

            messageType = (E)Enum.ToObject(typeof(E), type);
            
            return message;
        }

        /// <summary>
        /// Receives text message of an expected type
        /// </summary>
        /// <exception cref="UnexpectedMessageTypeException"></exception>
        public string ReceiveTextMessageType(int expectedType)
        {
            return Encoding.UTF8.GetString(
                ReceiveMessageType(expectedType)
            );
        }
        
        /// <summary>
        /// Receives text message of an expected type
        /// </summary>
        /// <exception cref="UnexpectedMessageTypeException"></exception>
        public string ReceiveTextMessageType(Enum expectedType)
        {
            return Encoding.UTF8.GetString(
                ReceiveMessageType(expectedType)
            );
        }
        
        //////////////////////////
        // Binary communication //
        //////////////////////////
        
        /// <summary>
        /// Sends an empty message of a given type to the other side
        /// </summary>
        public void SendMessage(int messageType)
            => SendMessage(messageType, null);
        
        /// <summary>
        /// Sends an empty message of a given type to the other side
        /// </summary>
        public void SendMessage(Enum messageType)
            => SendMessage(Convert.ToInt32(messageType), null);

        /// <summary>
        /// Sends a message to the other side
        /// </summary>
        public void SendMessage(int messageType, byte[] payload)
        {
            if (payload == null)
                payload = new byte[0];
            
            byte[] sizeHeader = BitConverter.GetBytes(payload.Length);
            if (BitConverter.IsLittleEndian)
                Array.Reverse(sizeHeader);

            byte[] typeHeader = BitConverter.GetBytes(messageType);
            if (BitConverter.IsLittleEndian)
                Array.Reverse(typeHeader);

            socket.Send(sizeHeader);
            socket.Send(typeHeader);
            socket.Send(payload);
        }

        /// <summary>
        /// Receives message of an expected type
        /// </summary>
        /// <exception cref="UnexpectedMessageTypeException"></exception>
        public byte[] ReceiveMessageType(int expectedType)
        {
            byte[] message = ReceiveMessage(out int actualType);

            if (actualType != expectedType)
            {
                throw new UnexpectedMessageTypeException(
                    $"Received message of type {actualType} " +
                    $"while expecting type {expectedType}.\n" +
                    "ASCII decoded message content is:\n" +
                    Encoding.ASCII.GetString(message)
                );
            }

            return message;
        }

        /// <summary>
        /// Receives message of an expected type
        /// </summary>
        /// <exception cref="UnexpectedMessageTypeException"></exception>
        public byte[] ReceiveMessageType(Enum expectedType)
            => ReceiveMessageType(Convert.ToInt32(expectedType));

        /// <summary>
        /// Receive a single message from the input stream
        /// </summary>
        public byte[] ReceiveMessage(out int messageType)
        {
            byte[] sizeHeader = ReadBytes(4);
            if (BitConverter.IsLittleEndian)
                Array.Reverse(sizeHeader);
            int size = BitConverter.ToInt32(sizeHeader, 0);

            byte[] typeHeader = ReadBytes(4);
            if (BitConverter.IsLittleEndian)
                Array.Reverse(typeHeader);
            messageType = BitConverter.ToInt32(typeHeader, 0);

            return ReadBytes(size);
        }

        /////////////////////////////////////////
        // Binary communication implementation //
        /////////////////////////////////////////

        /// <summary>
        /// Reads a given number of bytes from the input stream
        /// (joins individual segments as they arrive
        /// to return the requested byte block)
        /// </summary>
        private byte[] ReadBytes(int count)
        {
            if (count <= 0)
                return new byte[0];

            int received = 0;
            byte[] buffer = new byte[count];

            while (true)
            {
                int k = socket.Receive(
                    buffer,
                    received,
                    buffer.Length - received,
                    SocketFlags.None
                );
                received += k;

                if (k == 0)
                    throw new ConnectionEndedException(
                        "Not enough bytes available. Connection ended."
                    );

                if (received == buffer.Length)
                    break;

                if (received > buffer.Length)
                    throw new NetworkingException(
                        "Wrong count passed to the method."
                    );
            }

            return buffer;
        }
    }
}