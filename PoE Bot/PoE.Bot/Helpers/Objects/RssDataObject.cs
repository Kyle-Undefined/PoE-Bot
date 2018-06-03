namespace PoE.Bot.Helpers.Objects
{
    using System.Collections.Generic;
    using System.Xml.Serialization;

    [XmlRoot("rss")]
    public partial class RssDataObject
    {
        [XmlElement("channel")]
        public RssData Data { get; set; }
    }

    public partial class RssData
    {
        [XmlElement("item")]
        public List<RssItem> Items { get; set; }
    }

    public partial class RssItem
    {
        [XmlElement("title")]
        public string Title { get; set; }

        [XmlElement("link")]
        public string Link { get; set; }

        [XmlElement("description")]
        public string Description { get; set; }

        [XmlElement("pubDate")]
        public string PubDate { get; set; }
    }
}
