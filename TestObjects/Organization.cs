using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ProtoBuf;

namespace TestObjects
{
    [ProtoContract(ImplicitFields = ImplicitFields.AllFields)]
    public class Organization
    {
        public string Name { get; set; }
        public uint Id { get; set; }
    }
}
