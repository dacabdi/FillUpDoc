// <copyright file="Program.cs" company="Simoorg">
// Licensed under the MIT license. See LICENSE file in the samples root for full license information.
// </copyright>

namespace Simoorg.FillUpDoc
{
    using System;
    using System.IO;
    using DocumentFormat.OpenXml.Packaging;
    using DocumentFormat.OpenXml.Wordprocessing;
    using Newtonsoft.Json.Linq;
    using NLog;

    public static class Program
    {
        private static readonly Logger Logger = LogManager.GetCurrentClassLogger();

        public static int Main(string[] args)
        {
            Logger.Info("~~~~~~~~~~~~~~ Running simmorg session! ~~~~~~~~~~~~~~");

            if (args.Length <= 1)
            {
                Logger.Fatal("Did not provide enough arguments.");
                return 1;
            }

            string templatePath = args[0];
            string jsonPath = args[1];
            string outputPath = args[2];

            File.Copy(templatePath, outputPath, true);

            Logger.Info($"Using template file: {templatePath}");
            Logger.Info($"Using JSON file: {jsonPath}");
            Logger.Info($"Output to: {outputPath}");

            try
            {
                // open json file
                using WordprocessingDocument wordTemplate = WordprocessingDocument.Open(outputPath, true);
                Logger.Info("Parsing JSON");
                JObject json = JObject.Parse(File.ReadAllText(jsonPath));

                Logger.Info("Parsing Template Body");
                Body body = wordTemplate.MainDocumentPart.Document.Body;

                Logger.Info("Generating new Template Structure");
                TemplateTraversor generator = new StructureGenerator(body, json);
                generator.Traverse();

                Logger.Info("Merging JSON content into Template");
                TemplateTraversor merger = new TemplateJsonMerger(body, json);
                merger.Traverse();
            }
            catch (Exception ex)
            {
                Logger.Fatal(ex, "A fatal error ocurred.");
                Logger.Fatal(ex.StackTrace);
                return 1;
            }

            Logger.Info("Session ended!");

            return 0;
        }
    }
}
