namespace Simoorg.FillUpDoc
{
    using System;
    using System.Linq;
    using DocumentFormat.OpenXml;
    using DocumentFormat.OpenXml.Wordprocessing;
    using Newtonsoft.Json.Linq;
    using NLog;

    public class TemplateJsonMerger : TemplateTraversor
    {
        private static readonly Logger Logger = LogManager.GetCurrentClassLogger();

        public TemplateJsonMerger(OpenXmlElement xmlRoot, JObject jsonRoot)
            : base(xmlRoot, jsonRoot)
        {
        }

        protected override JToken Matcher(OpenXmlElement node, JToken json, string tag)
        {
            return this.GetJTokenFromKey((JContainer)json, this.stripper.Replace(tag, string.Empty));
        }

        protected override void VisitNode(OpenXmlElement element, JToken json, string tag, string strippedTag, int level)
        {
            switch (json.Type)
            {
                case JTokenType.Array:

                    Logger.Info($"Tag '{tag}' matched array '{json.Path}'");

                    try
                    {
                        int idx = int.Parse(this.stripper.Match(tag).Value);
                        this.DoTraverseAllChildren(element, json[idx], level + 1);
                    }
                    catch (Exception ex)
                    {
#pragma warning disable CS0618 // Type or member is obsolete
                        Logger.ErrorException($"Failed to read array index after stripping '{tag}'", ex);
#pragma warning restore CS0618 // Type or member is obsolete
                    }

                    break;

                case JTokenType.Object:

                    Logger.Info($"Tag '{tag}' matched object '{json.Path}'");

                    this.DoTraverseAllChildren(element, json, level + 1);

                    break;

                default:

                    Logger.Info($"Tag '{tag}' matched property '{json.Path}'='{json.Value<string>()}'");
                    if (ReplaceSdtContent((SdtElement)element, json, strippedTag))
                    {
                        Logger.Info($"Content control '{tag}' successfully replaced with property '{json.Path}'='{json.Value<string>()}'");
                    }
                    else
                    {
                        Logger.Warn($"Content control '{tag}' failed to be replaced with property '{json.Path}'='{json.Value<string>()}'");
                        Logger.Warn($"Unless this is a desired outcome, check content control xml structure and text field integrity (Activate Debug Mode)");
                        Logger.Debug($"{element.OuterXml}");
                    }

                    break;
            }
        }

        private static bool ReplaceSdtContent(SdtElement element, JToken json, string tag)
        {
            Text text = element.Descendants<Text>().Where(r => r.Text == tag).FirstOrDefault();
            if (text != null)
            {
                text.Text = json.Value<string>();
                return true;
            }

            return false;
        }
    }
}