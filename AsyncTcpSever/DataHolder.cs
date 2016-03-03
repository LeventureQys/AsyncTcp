using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AsyncTcpSever
{
    internal  class DataHolder
    {
        private byte[] dataMessageReceived;

        internal byte[] DataMessageReceived
        {
            get { return dataMessageReceived; }
            set { dataMessageReceived = value; }
        }
    }
}
