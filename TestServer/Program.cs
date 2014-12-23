﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ReactiveProtobuf.Protocol;
using ReactiveSockets;
using TestObjects;

namespace TestServer
{
    class Program
    {
        static void Main(string[] args)
        {
            var server = new ReactiveListener(41337);

            server.Connections.Subscribe(socket =>
            {
                Console.WriteLine("New socket connected {0}", socket.GetHashCode());

                var protocol = new ProtobufChannel<Person>(socket, true, true, "1234");

                protocol.Receiver.Subscribe(person =>
                {
                    Console.WriteLine("Person {0} {1} connected", person.FirstName, person.LastName);
                });
            });

            server.Start();

            Console.WriteLine("Press Enter to exit");
            Console.ReadLine();
        }
    }
}
