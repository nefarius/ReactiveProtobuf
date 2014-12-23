using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ProtoBuf;

namespace TestObjects
{
    [ProtoContract(ImplicitFields = ImplicitFields.AllFields)]
    public class Person
    {
        public string LastName { get; set; }
        public string FirstName { get; set; }
    }
}
