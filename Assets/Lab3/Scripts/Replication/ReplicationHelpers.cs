using System;
using UnityEngine;

namespace Delivery3
{
    public class TodoException : Exception
    {
        public int number;
        public TodoException(int number) : base("TODO " + number) { this.number = number; }
    }

    public class ReplicationManagerServerSolution : ReplicationManagerServer
    {
        public ReplicationManagerServerSolution(LinkingContext linking) : base(linking) { }
    }

    public class ReplicationManagerClientSolution : ReplicationManagerClient
    {
    }
}