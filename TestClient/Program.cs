using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Concurrency;
using System.Reactive.Linq;
using System.Security;
using System.Text;
using ReactiveProtobuf.Protocol;
using ReactiveSockets;
using TestObjects;

namespace TestClient
{
    class Program
    {
        static void Main(string[] args)
        {
            var client = new ReactiveClient("localhost", 41337);

            var protocol = new ProtobufChannel<Person>(client);

            protocol.Receiver.SubscribeOn(TaskPoolScheduler.Default).Subscribe(person =>
            {
                if (person != null)
                {
                    Console.WriteLine("Person {0} {1} received", person.FirstName, person.LastName);
                }
            });

            client.ConnectAsync().Wait();

            var p1 = new Person()
            {
                FirstName = "Fritz",
                LastName = "Phantom"
            };

            var p2 = new Person()
            {
                FirstName = "Tom",
                LastName = "Turbo"
            };

            protocol.SendAsync(p1);
            protocol.SendAsync(p2);

            Console.ReadLine();
        }
    }
}
