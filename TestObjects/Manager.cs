using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ProtoBuf;

namespace TestObjects
{
    [ProtoContract(ImplicitFields = ImplicitFields.AllFields)]
    public class Manager : Person
    {
        public uint Salary { get; set; }
    }
}
