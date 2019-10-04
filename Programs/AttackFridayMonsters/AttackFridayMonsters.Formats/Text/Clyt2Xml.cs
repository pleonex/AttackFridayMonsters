//  Copyright (c) 2019 SceneGate Team
//
//  This program is free software: you can redistribute it and/or modify
//  it under the terms of the GNU General Public License as published by
//  the Free Software Foundation, either version 3 of the License, or
//  (at your option) any later version.
//
//  This program is distributed in the hope that it will be useful,
//  but WITHOUT ANY WARRANTY; without even the implied warranty of
//  MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
//  GNU General Public License for more details.
//
//  You should have received a copy of the GNU General Public License
//  along with this program.  If not, see <http://www.gnu.org/licenses/>.
namespace AttackFridayMonsters.Formats.Text
{
    using System;
    using System.IO;
    using System.Xml.Linq;
    using AttackFridayMonsters.Formats.Text.Layout;
    using Yarhl.IO;
    using Yarhl.FileFormat;

    public class Clyt2Xml : IConverter<Clyt, BinaryFormat>
    {
        public BinaryFormat Convert(Clyt source)
        {
            if (source == null)
                throw new ArgumentNullException(nameof(source));

            var stream = new MemoryStream();
            BinaryFormat binary = new BinaryFormat(DataStreamFactory.FromStream(stream));

            XDocument xml = new XDocument(new XDeclaration("1.0", "utf-8", "yes"));
            XElement root = new XElement("clyt");
            xml.Add(root);

            root.Add(new XElement("children"));
            ExportPanel(root, source.RootPanel);

            xml.Save(stream);
            binary.Stream.Length = stream.Length;

            return binary;
        }

        void ExportPanel(XElement parent, Panel panel)
        {
            XElement xmlPanel = new XElement(GetPanelType(panel));
            xmlPanel.SetAttributeValue("name", panel.Name);
            xmlPanel.Add(Vector3ToXml("translation", panel.Translation));
            xmlPanel.Add(Vector3ToXml("rotation", panel.Rotation));
            xmlPanel.Add(Vector2ToXml("scale", panel.Scale));
            xmlPanel.Add(Vector2ToXml("size", panel.Size));

            xmlPanel.Add(new XElement("children"));
            foreach (var child in panel.Children) {
                ExportPanel(xmlPanel, child);
            }

            parent.Element("children").Add(xmlPanel);
        }

        static string GetPanelType(Panel panel)
        {
            switch (panel) {
                case TextSection text:
                    return "text";

                case Picture picture:
                    return "picture";

                case Window window:
                    return "window";

                default:
                    return "panel";
            }
        }

        static XElement Vector3ToXml(string name, Vector3 vector)
        {
            XElement xmlVector = new XElement(name);
            xmlVector.SetAttributeValue("x", vector.X);
            xmlVector.SetAttributeValue("y", vector.Y);
            xmlVector.SetAttributeValue("z", vector.Z);
            return xmlVector;
        }

        static XElement Vector2ToXml(string name, Vector2 vector)
        {
            XElement xmlVector = new XElement(name);
            xmlVector.SetAttributeValue("x", vector.X);
            xmlVector.SetAttributeValue("y", vector.Y);
            return xmlVector;
        }
    }
}