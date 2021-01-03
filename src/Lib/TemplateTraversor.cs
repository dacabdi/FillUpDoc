// <copyright file="TemplateTraversor.cs" company="Simoorg">
// Licensed under the MIT license. See LICENSE file in the samples root for full license information.
// </copyright>

namespace Simoorg.FillUpDoc
{
    using System.Collections.Generic;
    using System.Linq;
    using System.Text.RegularExpressions;
    using DocumentFormat.OpenXml;
    using DocumentFormat.OpenXml.Wordprocessing;
    using Newtonsoft.Json.Linq;
    using NLog;

    public abstract class TemplateTraversor
    {
        [System.Diagnostics.CodeAnalysis.SuppressMessage("StyleCop.CSharp.MaintainabilityRules", "SA1401:Fields should be private", Justification = "TODO")]
        protected readonly Regex stripper = new Regex(@"\d+$");

        private static readonly Logger Logger = LogManager.GetCurrentClassLogger();

        public TemplateTraversor(OpenXmlElement xmlRoot, JObject jsonRoot)
        {
            Logger.Trace("TemplateTraversor:Constructor invoked");
            this.JsonRoot = jsonRoot;
            this.XmlRoot = xmlRoot;
        }

        private JObject JsonRoot { get; set; }

        private OpenXmlElement XmlRoot { get; set; }

        public void Traverse()
        {
            Logger.Debug("Starting traversal");
            this.DoTraverse(this.XmlRoot, this.JsonRoot, 0);
        }

        protected abstract void VisitNode(OpenXmlElement node, JToken json, string tag, string strippedTag, int level);

        protected abstract JToken Matcher(OpenXmlElement node, JToken json, string tag);

        protected void DoTraverse(OpenXmlElement node, JToken json, int level)
        {
            Logger.Trace($"DoTraverse:[level={level} node.LocalName={node.LocalName} json.Type={json.Type} json.Path={json.Path}]");

            string tag = null;

            // if the node hasnt been visited
            // is sdt
            // and has a tag value
            // that matches a json key
            if (node.LocalName == "sdt" && (tag = this.GetSdtElementTagValue((SdtElement)node)) != null)
            {
                JToken matchingJson = null;
                if ((matchingJson = this.Matcher(node, json, tag)) != null)
                {
                    string strippedTag = this.stripper.Replace(tag, string.Empty);
                    this.VisitNode(node, matchingJson, tag, strippedTag, level + 1);
                    return;
                }
                else
                {
                    Logger.Warn($"Content Control with tag={tag} did not match any JSON entry under path={json.Path}");
                }
            }

            this.DoTraverseAllChildren(node, json, level + 1);
        }

        protected void DoTraverseAllChildren(OpenXmlElement node, JToken json, int level)
        {
            foreach (OpenXmlElement child in node.Elements<OpenXmlCompositeElement>())
            {
                this.DoTraverse(child, json, level);
            }
        }

        protected JToken GetJTokenFromKey(JContainer searchRoot, string key)
        {
            List<JProperty> prop = searchRoot.DescendantsAndSelf()
                                    .OfType<JProperty>()
                                    .Where(p => p.Name.ToString() == key).ToList();

            return prop.Count == 0 ? null : this.JsonRoot.SelectToken(prop.First().Path, true);
        }

        protected DocumentFormat.OpenXml.Wordprocessing.Tag GetSdtElementTag(SdtElement sdtElement)
        {
            return sdtElement.SdtProperties.Elements<Tag>().SingleOrDefault();
        }

        protected Tag SetSdtElementTag(SdtElement sdtElement, Tag newTag)
        {
            Tag oldTag = sdtElement.SdtProperties.Elements<Tag>().SingleOrDefault();
            oldTag = newTag;
            return newTag;
        }

        protected string GetSdtElementTagValue(SdtElement sdtElement)
        {
            Tag tag = this.GetSdtElementTag(sdtElement);
            return tag?.Val;
        }

        protected string SetSdtElementTagValue(SdtElement sdtElement, string value)
        {
            Tag tag = this.GetSdtElementTag(sdtElement);
            return (tag != null) ? tag.Val = value : null;
        }
    }
}