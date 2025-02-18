using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TradingExpertAdvisor.Models
{
    public class UnsolicitedMessage
    {
        public MessageType Type { get; set; }
        public string Message { get; set; }

        public UnsolicitedMessage(MessageType type, string message)
        {
            this.Type = type;
            this.Message = message;
        }
    }
}
