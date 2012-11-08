using System.Xml.Serialization;

namespace Gqqnbig.TrafficVolumeMonitor.Testing
{
    public class AnalyzeOutput
    {
        [XmlAttribute]
        public int Id { get; set; }
        [XmlAttribute]
        public int CarNumber { get; set; }

        public override string ToString()
        {
            return string.Format("Id:{0}; Car number:{1}", Id, CarNumber);
        }
    }
}