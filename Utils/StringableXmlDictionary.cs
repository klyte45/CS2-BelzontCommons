using System.Collections.Generic;

using System.Xml.Serialization;


namespace Belzont.Utils
{
    [XmlRoot("StringableXmlDictionary")]

    public class StringableXmlDictionary : Dictionary<string, string>, IXmlSerializable
    {
        #region IXmlSerializable Members

        public System.Xml.Schema.XmlSchema GetSchema() => null;



        public void ReadXml(System.Xml.XmlReader reader)

        {
            if (reader.IsEmptyElement)
            {
                reader.Read();
                return;
            }
            var valueSerializer = new XmlSerializer(typeof(EntryStringableXmlDictionary), "");
            reader.ReadStartElement();
            while (reader.NodeType != System.Xml.XmlNodeType.EndElement)
            {
                if (reader.NodeType != System.Xml.XmlNodeType.Element)
                {
                    reader.Read();
                    continue;
                }

                var value = (EntryStringableXmlDictionary)valueSerializer.Deserialize(reader);
                Add(value.Id, value.Value ?? null);

            }

            reader.ReadEndElement();


        }



        public void WriteXml(System.Xml.XmlWriter writer)

        {

            var valueSerializer = new XmlSerializer(typeof(EntryStringableXmlDictionary), "");

            var ns = new XmlSerializerNamespaces();
            ns.Add("", "");
            foreach (var key in Keys)
            {
                var value = this[key];
                valueSerializer.Serialize(writer, new EntryStringableXmlDictionary()
                {
                    Id = key,
                    Value = value
                }, ns);
                ;
            }

        }


        #endregion

    }
    [XmlRoot("Entry")]
    public class EntryStringableXmlDictionary
    {
        [XmlAttribute("key")]
        public string Id { get; set; }

        [XmlAttribute("value")]
        public string Value { get; set; }
    }

}
