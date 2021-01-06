namespace log4net_logdna.UnitTests.Models
{
    using log4net.Core;

    public class FixedComplexType : IFixingRequired
    {
        public string PropertyOne { get; set; }

        public int PropertyTwo { get; set; }

        public object GetFixedObject()
        {
            return "I'm a fixed type!";
        }
    }
}