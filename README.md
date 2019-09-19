# Parrot 🦜

Simple messaging library on top of TCP written in C#

## Connecting a client

```csharp
using (Client client = Client.Connect("127.0.0.1", 1234))
{
    // here we can do stuff
}
```

## Sending messages

```csharp
// integer message ID and byte[] payload
client.SendMessage(42, new byte[0]);

// UTF-8 encoded on sending, ideal for JSON
client.SendTextMessage(42, "Hello!");

// empty message body (an ack for example)
client.SendMessage(42);
```

## Receiving messages

```csharp
byte[] msg = client.ReceiveMessage(out int messageType);
// throws ConnectionEndedException when the other side closed already
// throws NetworkingException on any other problem
```

Or receive message of a given type and throw on different message.

```csharp
byte[] msg = client.ReceiveMessageType(42);
// throws UnexpectedMessageTypeException
```

Or text messages.

```csharp
string msg = client.ReceiveTextMessage(out int messageType);
string msg = client.ReceiveTextMessageType(42);
```

## Starting a server

```csharp
Action<Client> HandleNewConnection = (client) => {
    // Do stuff with the client.
    // Runs in a separate thread.
};

Server server = new Server("0.0.0.0", 1234, HandleNewConnection);
server.Start();

// implement your custom wait till terminated here

server.Stop();
```

## Protocol

Protocol uses TCP byte stream. It sends messages on top of it.

A message has 8 byte header and then a body.

Bytes 0-3 are a big-endian integer containing size of the body in bytes.

Bytes 4-7 are a big-endian integer describing type of the message.
Message types are user-defined.

Remaining bytes 5-.. are the body of the message. Body might be empty.

When sending strings, they are serialized using the UTF-8 encoding.
