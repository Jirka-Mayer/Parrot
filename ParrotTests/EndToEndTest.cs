using System;
using System.Diagnostics;
using NUnit.Framework;
using Parrot;

namespace ParrotTests
{
    [TestFixture]
    public class EndToEndTest
    {
        [TestCase]
        public void ServerCanEcho()
        {
            var server = new Server("127.0.0.1", 16123, client => {
                try
                {
                    while (true)
                    {
                        string msg = client.ReceiveTextMessage(out int type);
                        client.SendTextMessage(type, msg + " | " + msg);
                    }
                }
                catch (ConnectionEndedException)
                {
                }
            });
            
            server.Start();

            using (var client = Client.Connect("127.0.0.1", 16123))
            {
                client.SendTextMessage(42, "Hello server!");

                string msg = client.ReceiveTextMessage(out int type);
                
                Assert.AreEqual(42, type);
                Assert.AreEqual("Hello server! | Hello server!", msg);
            }
            
            server.Stop();
        }
        
        [TestCase]
        [Ignore("I used this test to test socket.NoDelay effectiveness.")]
        public void TestNoDelaySpeedIncrease()
        {
            var server = new Server("127.0.0.1", 16123, client => {
                try
                {
                    while (true)
                    {
                        string msg = client.ReceiveTextMessage(out int type);
                        client.SendTextMessage(type, msg);
                    }
                }
                catch (ConnectionEndedException)
                {
                }
            });
            
            server.Start();

            using (var client = Client.Connect("127.0.0.1", 16123))
            {
                Stopwatch sw = new Stopwatch();
                
                sw.Start();
                
                for (int i = 0; i < 1000; i++)
                {
                    client.SendTextMessage(42, "Hello server!");
                    client.ReceiveTextMessage(out int _);
                }
                
                sw.Stop();
                
                Console.WriteLine(sw.ElapsedMilliseconds + "ms");
            }
            
            server.Stop();
        }
    }
}