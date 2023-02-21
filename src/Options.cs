using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using System.Web;
using Menu.Remix.MixedUI;
using UnityEngine;
using static System.Net.Mime.MediaTypeNames;

namespace OldMSCScenes
{
    internal class Options : OptionInterface
    {
        public static Options instance = new Options();

        private Vector2 pos;

        private readonly float spacing = 50f;

        private readonly float fontSize = 20f;

        private readonly float checkBoxSize = 25f;

        private readonly List<OpLabel> realCheckBoxLabels = new List<OpLabel>();



        public override void Initialize()
        {
            base.Initialize();

            OnConfigChanged += ConfigChanged;
            OnConfigReset += ConfigChanged;

            Tabs = new OpTab[1];
            Tabs[0] = new OpTab(this, "Options");

            tex = new Texture2D(2, 2); // Create an empty Texture; size doesn't matter (she said)

            imagePreview = new OpImage(new Vector2(275, 400), tex);

            Tabs[0].AddItems(new UIelement[] { imagePreview });

            pos = new Vector2(25f, 400f);
            foreach (Configurable<bool> check in configurables)
            {
                float lineX = pos.x;

                OpSimpleButton opSimpleButton = new OpSimpleButton(new Vector2(lineX, pos.y), new Vector2(100f, fontSize), "Preview");
                opSimpleButton.description = $"Preview the illustration for {check.key}";

                opSimpleButton.OnClick += previewClick => ChangeImage(check.key);

                lineX += 100f + 0.25f * spacing;

                OpCheckBox opCheckBox = new OpCheckBox(check, new Vector2(lineX, pos.y));
                opCheckBox.description = check.info.description;

                lineX += checkBoxSize + 0.25f * spacing;

                OpLabel opLabel = new OpLabel(
                    new Vector2(lineX + 10f, pos.y + 3f), 
                    new Vector2(100f, fontSize), 
                    check.key, FLabelAlignment.Left, false, null);
                
                Tabs[0].AddItems(new UIelement[]
                {
                    opSimpleButton,
                    opCheckBox,
                        opLabel
                });
                realCheckBoxLabels.Add(opLabel);
                pos.y -= spacing;
            }
            ResetHold();

        }

        private void ConfigChanged()
        {
            OldMSCScenes.SafariIconToggle();
        }

        private void ChangeImage(string n)
        {
            string filename = AssetManager.ResolveFilePath($"scenes{System.IO.Path.DirectorySeparatorChar}OLDLandscape - {n}{System.IO.Path.DirectorySeparatorChar}OLDLandscape - {n} - Flat - mini.png");
            if (!System.IO.File.Exists(filename)) { return; }

            var rawData = System.IO.File.ReadAllBytes(filename);
            tex = new Texture2D(2, 2); // Create an empty Texture; size doesn't matter (she said)

            tex.LoadImage(rawData);
            imagePreview.ChangeImage(tex);
        }

        private OpHoldButton homeHold;

        private OpImage imagePreview;

        private Texture2D tex;

        private void ResetHold()
        {
            if (homeHold != null)
            { OpTab.DestroyItems(new UIelement[] { homeHold }); }

            homeHold = new OpHoldButton(new Vector2(230f, 150f), new Vector2(180f, 30f), spoilers? "Hide Region Names" : "Show Region Names", 40f);
            homeHold.OnPressDone += ToggleSpoilers;
            Tabs[0].AddItems(new UIelement[] { homeHold });

        }

        private void ToggleSpoilers(UIfocusable trigger)
        {
            spoilers = !spoilers;
            ResetHold();

            foreach (OpLabel label in realCheckBoxLabels)
                {
                if (OldMSCScenes.regionnames.ContainsKey(label.text) && spoilers)
                { label.text = OldMSCScenes.regionnames[label.text]; }

                else if (!spoilers)
                {
                    foreach (KeyValuePair<string, string> name in OldMSCScenes.regionnames)
                    {
                        if (name.Value == label.text)
                        { label.text = name.Key; break; }
                    }
                }
            }
        }


        private bool spoilers = false;

        public static List<Configurable<bool>> configurables = new List<Configurable<bool>>();

        public static void GenerateList()
        {
            foreach (string region in OldMSCScenes.regionnames.Keys)
            {
                configurables.Add(instance.config.Bind(region, region != "LC" && region != "LM", new ConfigurableInfo($"Retro Landscape for region {region}", null, "", Array.Empty<object>())));
            }
        }
    }
}
