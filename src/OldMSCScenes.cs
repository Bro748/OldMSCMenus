using BepInEx;
using System.Security.Permissions;
using System.Collections.Generic;
using MonoMod.RuntimeDetour;
using System;
using System.IO;
using System.Text.RegularExpressions;
using UnityEngine;
using System.Globalization;
using System.Linq;

// Allows access to private members
#pragma warning disable CS0618
[assembly: SecurityPermission(SecurityAction.RequestMinimum, SkipVerification = true)]
#pragma warning restore CS0618

namespace OldMSCScenes;

[BepInPlugin("com.author.testmod", "Test Mod", "0.1.0")]
sealed class OldMSCScenes : BaseUnityPlugin
{
    bool init;

    public void OnEnable()
    {
        // Add hooks here
        On.RainWorld.OnModsInit += OnModsInit;
    }

    private void OnModsInit(On.RainWorld.orig_OnModsInit orig, RainWorld self)
    {
        try
        {
            orig(self);

            Options.GenerateList();
            global::MachineConnector.SetRegisteredOI(MOD_ID, Options.instance);

            SafariIconToggle();

            if (init) return;

            init = true;

            On.Menu.MenuScene.BuildHRLandscapeScene += MenuScene_BuildOldLandscapeScene;
            On.Menu.MenuScene.BuildLCLandscapeScene += MenuScene_BuildOldLandscapeScene;
            On.Menu.MenuScene.BuildLMLandscapeScene += MenuScene_BuildOldLandscapeScene;
            On.Menu.MenuScene.BuildMSLandscapeScene += MenuScene_BuildOldLandscapeScene;
            On.Menu.MenuScene.BuildDMLandscapeScene += MenuScene_BuildOldLandscapeScene;

        }
        catch (Exception e) { throw e; }
    }
    #region buildscene
    private void MenuScene_BuildOldLandscapeScene<T, U>(T orig, U self) 
        where T : Delegate 
        where U : Menu.MenuScene
    {
        string[] id = self.sceneID.ToString().Split('_');

        if (id.Length == 2)
        {
            if (regionCheck(id[1]))
            {
                BuildCustomScene2(self, $"OLDLandscape - {id[1]}");
                return;
            }
        }
        orig.DynamicInvoke(self);
    }

    public static bool regionCheck(string region)
    {
        foreach (Configurable<bool> check in Options.configurables)
        {
            if (check.key == region)
            {
                return check.Value;
            }
        }
        return false;
    }

    public static void BuildCustomScene2(Menu.MenuScene scene, string sceneFolder)
    {
        string[] array = Regex.Split(sceneFolder, " - ");

        string fileName = sceneFolder;
        string regionAcronym = array[1];
        scene.blurMin = -0.1f;
        scene.blurMax = 0.5f;

        scene.sceneFolder = "Scenes" + Path.DirectorySeparatorChar.ToString() + fileName;

        if (!Directory.Exists(AssetManager.ResolveDirectory(scene.sceneFolder)) || Directory.GetFiles(AssetManager.ResolveDirectory(scene.sceneFolder)).Length == 0) { goto LandscapeTitle; }


        if (scene.flatMode)
        {
            scene.AddIllustration(new Menu.MenuIllustration(scene.menu, scene, scene.sceneFolder, fileName + " - Flat", new Vector2(683f, 384f), false, true));
            goto LandscapeTitle;
        }

        string path = scene.sceneFolder + Path.DirectorySeparatorChar.ToString() + fileName + ".txt";

        if (!File.Exists(AssetManager.ResolveFilePath(path))) { goto LandscapeTitle; }

        foreach (string line in File.ReadAllLines(AssetManager.ResolveFilePath(path)))
        {
            string[] array2 = Regex.Split(line, " : ");

            if (array2.Length == 0 || array2[0].Length == 0) { continue; }

            if (array2[0] == "blurMin" && array2.Length >= 2) { scene.blurMin = float.Parse(array2[1]); }
            else if (array2[0] == "blurMax" && array2.Length >= 2) { scene.blurMax = float.Parse(array2[1]); }
            else if (array2[0] == "idleDepths" && array2.Length >= 2 && float.TryParse(array2[1], out float idleResult)) { (scene as Menu.InteractiveMenuScene)?.idleDepths.Add(idleResult); }
            else
            {
                if (File.Exists(AssetManager.ResolveFilePath(scene.sceneFolder + Path.DirectorySeparatorChar.ToString() + array2[0] + ".png")))
                {
                    scene.AddIllustration(new Menu.MenuDepthIllustration(
                        scene.menu, scene, scene.sceneFolder, array2[0], new Vector2(0f, 0f),
                        (array2.Length >= 2 && float.TryParse(array2[1], out float r) ? r : 1),
                        (array2.Length >= 3 && ExtEnumBase.TryParse(typeof(Menu.MenuDepthIllustration.MenuShader), array2[2], false, out ExtEnumBase result) ? (Menu.MenuDepthIllustration.MenuShader)result : Menu.MenuDepthIllustration.MenuShader.Normal)
                        ));
                }
            }
        }

        LoadPositions(scene);


    LandscapeTitle:;
        if (scene.menu.ID == ProcessManager.ProcessID.FastTravelScreen || scene.menu.ID == ProcessManager.ProcessID.RegionsOverviewScreen)
        {
            scene.AddIllustration(new Menu.MenuIllustration(scene.menu, scene, string.Empty, $"Title_{regionAcronym}_Shadow", new Vector2(0.01f, 0.01f), true, false));
            scene.AddIllustration(new Menu.MenuIllustration(scene.menu, scene, string.Empty, $"Title_{regionAcronym}", new Vector2(0.01f, 0.01f), true, false));
            scene.flatIllustrations[scene.flatIllustrations.Count - 1].sprite.shader = scene.menu.manager.rainWorld.Shaders["MenuText"];
        }

    }


    public static void LoadPositions(Menu.MenuScene scene)
    {

        string path2 = AssetManager.ResolveFilePath(scene.sceneFolder + Path.DirectorySeparatorChar.ToString() + "positions_ims.txt");
        if (!File.Exists(path2) || !(scene is Menu.InteractiveMenuScene))
        {
            path2 = AssetManager.ResolveFilePath(scene.sceneFolder + Path.DirectorySeparatorChar.ToString() + "positions.txt");
        }
        if (File.Exists(path2))
        {
            string[] array3 = File.ReadAllLines(path2);
            int num3 = 0;
            while (num3 < array3.Length && num3 < scene.depthIllustrations.Count)
            {
                scene.depthIllustrations[num3].pos.x = float.Parse(Regex.Split(RWCustom.Custom.ValidateSpacedDelimiter(array3[num3], ","), ", ")[0], NumberStyles.Any, CultureInfo.InvariantCulture);
                scene.depthIllustrations[num3].pos.y = float.Parse(Regex.Split(RWCustom.Custom.ValidateSpacedDelimiter(array3[num3], ","), ", ")[1], NumberStyles.Any, CultureInfo.InvariantCulture);
                scene.depthIllustrations[num3].lastPos = scene.depthIllustrations[num3].pos;
                num3++;
            }
        }

        path2 = AssetManager.ResolveFilePath(scene.sceneFolder + Path.DirectorySeparatorChar.ToString() + "depths.txt");

        if (File.Exists(path2))
        {
            string[] array = File.ReadAllLines(path2);
            int num2 = 0;
            while (num2 < array.Length && num2 < scene.depthIllustrations.Count)
            {
                scene.depthIllustrations[num2].depth = float.Parse(array[num2]);
                num2++;
            }
        }
    }

    public static void SafariIconToggle()
    {
        try
        {
            foreach (ModManager.Mod mod in ModManager.ActiveMods)
            {
                if (mod.id != MOD_ID) { continue; }

                string direct = Path.Combine(mod.path, "illustrations");
                if (Directory.Exists(direct))
                {
                    foreach (string path in Directory.GetFiles(direct))
                    {
                        string[] fileName = Path.GetFileNameWithoutExtension(path).Split('_');
                        if (fileName.Length < 2) { continue; }

                        string region = fileName[1];

                        bool active = regionCheck(region);

                        if (active && fileName.ToString() != $"Safari_{region}")
                        { System.IO.File.Move(path, Path.Combine(direct, $"Safari_{region}.png")); }

                        else if (!active && fileName.ToString() != $"Safari_{region}_unused")
                        { System.IO.File.Move(path, Path.Combine(direct, $"Safari_{region}_unused.png")); }
                    }
                }
            }
        }catch (Exception e) { throw e; }
    }

    #endregion buildscene

    public static readonly string MOD_ID = "bro.retroillustrations";


    public static Dictionary<string, string> regionnames = new Dictionary<string, string>()
    {
        { "LC", "Metropolis" },
        { "LM", "Waterfront Facility" },
        { "DM", "Looks to the Moon" },
        { "MS", "Submerged Superstructure" },
        { "HR", "Rubicon" }
    };
}
