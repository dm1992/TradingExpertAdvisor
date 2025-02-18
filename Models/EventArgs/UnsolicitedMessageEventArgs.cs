using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TradingExpertAdvisor.Models.EventArgs
{
    public class UnsolicitedMessageEventArgs : BaseEventArgs
    {
        public UnsolicitedMessage UnsolicitedMessage { get; set; }

        public UnsolicitedMessageEventArgs(UnsolicitedMessage unsolicitedMessage)
        {
            this.UnsolicitedMessage = unsolicitedMessage;
        }
    }
}
