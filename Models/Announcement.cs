using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace TradingExpertAdvisor.Models
{
    public class Announcement
    {
        public string Title { get; set; }

        public string Description { get; set; }

        public string Url { get; set; }

        public DateTime Timestamp { get; set; }

        public DateTime? PublishTime { get; set; }

        public DateTime? StartTimestamp { get; set; }

        public DateTime? EndTimestamp { get; set; }

        public List<string> Tags { get; set; }
    }
}
