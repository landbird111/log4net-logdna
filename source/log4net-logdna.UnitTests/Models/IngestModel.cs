using System;
using System.Collections.Generic;
using System.Text;

namespace log4net_logdna.UnitTests.Models
{
    
    public class IngestModel
    {
        public Line[] lines { get; set; }
    }

    public class Line
    {
        public string line { get; set; }
        public string app { get; set; }
        public string level { get; set; }
        public string env { get; set; }
        public Meta meta { get; set; }
    }

    public class Meta
    {
        public Customfield customfield { get; set; }
    }

    public class Customfield
    {
        public string nestedfield { get; set; }
    }
}
