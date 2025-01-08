using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TradingExpertAdvisor.Models.EventArgs
{
    public class UnsolicitedMessageEventArgs : BaseEventArgs
    {
        public MessageType Type { get; set; }
        public string Message { get; set; }

        public UnsolicitedMessageEventArgs(string message) : this(MessageType.Info, message)
        {

        }

        public UnsolicitedMessageEventArgs(MessageType type, string message)
        {
            Type = type;
            Message = message;
        }
    }
}
