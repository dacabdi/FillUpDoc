// <copyright file="StructureGenerator.cs" company="Simoorg">
// Licensed under the MIT license. See LICENSE file in the samples root for full license information.
// </copyright>

namespace Simoorg.FillUpDoc
{
    using System.Linq;
    using DocumentFormat.OpenXml;
    using DocumentFormat.OpenXml.Wordprocessing;
    using Newtonsoft.Json.Linq;
    using NLog;

    public class StructureGenerator : TemplateTraversor
    {
        private static readonly Logger Logger = LogManager.GetCurrentClassLogger();

        public StructureGenerator(OpenXmlElement xmlRoot, JObject jsonRoot)
            : base(xmlRoot, jsonRoot)
        {
        }

        protected override JToken Matcher(OpenXmlElement node, JToken json, string tag)
        {
            return this.GetJTokenFromKey((JContainer)json, tag);
        }

        protected override void VisitNode(OpenXmlElement node, JToken json, string tag, string strippedTag, int level)
        {
            Logger.Trace($"StructureGenerator.VisitNode:[json.Type={json.Type}]");
            switch (json.Type)
            {
                case JTokenType.Array:

                    Logger.Info($"Tag '{tag}' matched array '{json.Path}'");

                    OpenXmlElement parent = node.Parent;
                    int count = json.Count();

                    // if empty array, delete current entry
                    if (count == 0)
                    {
                        Logger.Warn($"Array matching '{tag}' is empty, removing content control");
                        parent.RemoveChild(node);
                    }
                    else
                    {
                        Logger.Info($"Retagging template's original content control ");
                        Logger.Info($"Cloning and appending '{tag}' {count} times");

                        node = parent.RemoveChild(node);

                        // parentNode = parent.RemoveChild(node);

                        // otherwise, repeat as needed, and attach to parent
                        for (int i = 0; i < count; ++i)
                        {
                            OpenXmlElement clone = node.CloneNode(true); // OpenXmlElement clone = parentNode.CloneNode(true);

                            this.DoTraverseAllChildren(clone, json[i], level + 1);
                            this.SetSdtElementTagValue((SdtElement)clone, $"{tag}{i}");
                            parent.AppendChild(clone);
                        }
                    }

                    break;

                case JTokenType.Object:

                    Logger.Info($"Tag '{tag}' matched object '{json.Path}'");

                    if (json.Count() == 0)
                    {
                        Logger.Warn($"JSON Object is empty, removing content control");
                        node.Parent.RemoveChild(node);
                    }
                    else
                    {
                        this.DoTraverseAllChildren(node, json, level);
                    }

                    break;

                case JTokenType.Null:

                    Logger.Warn($"Tag '{tag}' matched null object '{json.Path}', removing content control");
                    node.Parent.RemoveChild(node);

                    break;

                default:

                    Logger.Debug($"Tag '{tag}' matched '{json.Path}', non structural content control, leave as is.");

                    break;
            }
        }
    }
}