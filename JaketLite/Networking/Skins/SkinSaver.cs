using PluginConfig.API;
using PluginConfig.API.Decorators;
using PluginConfig.API.Functionals;
using Polarite.Multiplayer;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using System.IO;
using Polarite.Debugging;
using System.Net.Http.Headers;
using UnityEngine.AddressableAssets;

namespace Polarite.Networking.Skins
{
    public struct SaveableSkin
    {
        public string name;
        public Skin data;
    }
    public class SkinPanel
    {
        public ConfigPanel panel;
        public ConfigHeader loadHeader;
        public ButtonField loadButton;
        public ButtonField deleteButton;
        public SaveableSkin save;

        public SkinPanel(SaveableSkin save, string folder, ConfigPanel dir)
        {
            save.name = SkinSaver.MakeValidFileName(save.name);
            int randomSuffix = UnityEngine.Random.Range(0, int.MaxValue);
            panel = new ConfigPanel(dir, save.name, $"skin.{save.name}.{randomSuffix}", ConfigPanel.PanelFieldType.StandardWithIcon);
            loadHeader = new ConfigHeader(panel, "<color=red>This will <b>overwrite</b> your <b>custom skin configuration!</b></color>");
            loadButton = new ButtonField(panel, "Load", panel.guid + ".load." + randomSuffix);
            deleteButton = new ButtonField(panel, "Delete", panel.guid + ".del." + randomSuffix);

            loadButton.onClick += () => SkinSaver.LoadSkin(save.name);
            deleteButton.onClick += () =>
            {
                panel.hidden = true;
                SkinSaver.DeleteSkin(save.name, folder);
            };
            this.save = save;
        }
    }
    public static class SkinSaver
    {
        public static string Path = Application.persistentDataPath + "/Skins/";
        public static string SkinSavePath = Application.persistentDataPath + "/Skins/Saved/";
        public static string SkinImportPath = Application.persistentDataPath + "/Skins/Imported/";
        public static string SkinPresetPath = Application.persistentDataPath + "/Skins/Presets/";
        public static Dictionary<string, SaveableSkin> Skins = new Dictionary<string, SaveableSkin>();
        public static Dictionary<string, SkinPanel> Panels = new Dictionary<string, SkinPanel>();
        public static Dictionary<string, ConfigPanel> FolderPanels = new Dictionary<string, ConfigPanel>();

        public static SaveableSkin CurrentSkin;

        public static void Clear()
        {
            Skins.Clear();
            foreach(var pan in Panels.Values)
            {
                pan.panel.hidden = true;
            }
            foreach(var fol in FolderPanels.Values)
            {
                fol.hidden = true;
            }
            Panels.Clear();
            FolderPanels.Clear();
        }
        public static string MakeValidFileName(string name)
        {
            foreach (char c in System.IO.Path.GetInvalidFileNameChars())
            {
                name = name.Replace(c, '_');
            }
            return name;
        }

        public static void Init()
        {
            if(!Directory.Exists(Path))
            {
                Directory.CreateDirectory(Path);
            }
            if(!Directory.Exists(SkinSavePath))
            {
                Directory.CreateDirectory(SkinSavePath);
            }
            if (!Directory.Exists(SkinImportPath))
            {
                Directory.CreateDirectory(SkinImportPath);
            }
            if (!Directory.Exists(SkinPresetPath))
            {
                Directory.CreateDirectory(SkinPresetPath);
                InitPresets();
            }
        }
        public static void InitPresets()
        {
            string basePresetPath = System.IO.Path.Combine(Directory.GetParent(ItePlugin.Instance.Info.Location).FullName, "skinpresets/");
            if(Directory.Exists(basePresetPath))
            {
                foreach(var file in Directory.GetFiles(basePresetPath, "*.polarskin"))
                {
                    File.Copy(file, System.IO.Path.Combine(SkinPresetPath, System.IO.Path.GetFileName(file)));
                    File.Delete(file);
                }
                Directory.Delete(basePresetPath);
            }
            else
            {
                InitPresets2();
            }
        }
        public static void InitPresets2()
        {
            foreach (var file in Directory.GetFiles(Directory.GetParent(ItePlugin.Instance.Info.Location).FullName, "*.polarskin"))
            {
                File.Copy(file, System.IO.Path.Combine(SkinPresetPath, System.IO.Path.GetFileName(file)));
                File.Delete(file);
            }
        }
        /// <summary>
        /// Makes a skin panel.
        /// </summary>
        /// <param name="name"></param>
        /// <param name="folderPath"></param>
        /// <param name="parentFolder"></param>
        /// <param name="currentCount">Required by SearchAndImport() to display a count</param>
        public static void MakePanel(string name, string folderPath, string parentFolder = "")
        {
            string validName = MakeValidFileName(name);
            string folderName = MakeValidFileName(System.IO.Path.GetFileName(folderPath));
            if (!FolderPanels.ContainsKey(folderName) && folderPath != Path)
            {
                FolderPanels[folderName] = new ConfigPanel(string.IsNullOrEmpty(parentFolder) ? ItePlugin.savePanel : FolderPanels[parentFolder], folderName, $"folder.{folderName}.{UnityEngine.Random.Range(0, int.MaxValue)}", ConfigPanel.PanelFieldType.StandardWithIcon);
                FolderPanels[folderName].icon = Addressables.LoadAssetAsync<Sprite>("Assets/Textures/UI/foldericon.png").WaitForCompletion();
            }
            if (!Panels.ContainsKey(validName))
            {
                SaveableSkin readData = ReadSkin(validName, folderPath);
                // incase it's a safe skin
                validName = MakeValidFileName(readData.name);
                SkinPanel panel = new SkinPanel(readData, folderPath + "/", (folderPath != Path) ? FolderPanels[folderName] : ItePlugin.savePanel);
                Panels[validName] = panel;
                panel.panel.icon = SkinManagerV2.MakeIcon(readData.data.Base, readData.data.Light, readData.data.Metal, readData.data.WingLight);
            }
        }
        public static void MakeFolderPanel(string folderPath, string parentFolder = "")
        {
            string folderName = MakeValidFileName(System.IO.Path.GetFileName(folderPath));
            if (!FolderPanels.ContainsKey(folderName) && folderPath != Path)
            {
                FolderPanels[folderName] = new ConfigPanel(string.IsNullOrEmpty(parentFolder) ? ItePlugin.savePanel : FolderPanels[parentFolder], folderName, $"folder.{folderName}.{UnityEngine.Random.Range(0, int.MaxValue)}", ConfigPanel.PanelFieldType.StandardWithIcon);
                FolderPanels[folderName].icon = Addressables.LoadAssetAsync<Sprite>("Assets/Textures/UI/foldericon.png").WaitForCompletion();
            }
        }
        public static void SaveSkin(SaveableSkin save, bool secret = false)
        {
            string validName = MakeValidFileName(save.name);
            save.name = validName;
            if (!Skins.ContainsKey(validName))
            {
                Skins.Add(validName, save);
            }
            else
            {
                Skins[validName] = save;
            }
            // use a packet writer since it does bytes aswell
            PacketWriter writer = new PacketWriter();
            writer.WriteSkin(save.data);
            File.WriteAllBytes(SkinSavePath + validName + ".polarskin", writer.GetBytes());
            if(!secret)
            {
                MakePanel(validName, Application.persistentDataPath + "/Skins/Saved");
                Application.OpenURL(SkinSavePath);
            }
        }
        public static SaveableSkin ReadSkin(string name, string pathA)
        {
            string path = pathA + "/";
            if (File.Exists(path + name + ".polarskin"))
            {
                try
                {
                    byte[] data = File.ReadAllBytes(path + name + ".polarskin");
                    BinaryPacketReader reader = new BinaryPacketReader(data, data.Length);
                    Skin skin = reader.ReadSkin();
                    SaveableSkin save = new SaveableSkin()
                    {
                        name = name,
                        data = skin
                    };
                    Skins[name] = save;
                    return save;
                }
                catch
                {
                    byte[] data = File.ReadAllBytes(path + name + ".polarskin");
                    string newName = name + " (Fixed)";
                    SaveableSkin skin = new SaveableSkin()
                    {
                        name = newName,
                        data = GetSafeSkin(data, name)
                    };
                    Skins[newName] = skin;
                    Logs.Warn($"Detected potentially outdated skin {name}, The skin has been remade.");
                    DeleteSkin(name, path);
                    SaveSkin(skin, true);
                    return skin;
                }
            }
            return new SaveableSkin()
            {
                name = name,
                data = ItePlugin.Instance.DefaultSkin()
            };
        }
        public static Skin GetSafeSkin(byte[] data, string skinName)
        {
            Skin newSkin = new Skin();
            BinaryPacketReader reader = new BinaryPacketReader(data, data.Length);
            Color baseCol = reader.ReadColor();
            Color baseLightCol = reader.ReadColor();
            Color wingLightCol = reader.ReadColor();
            string namePlate = skinName.Substring(0, Mathf.Clamp(skinName.Length, 0, 5)).ToUpper();
            newSkin = ItePlugin.Instance.GetSkin(baseCol, baseLightCol, wingLightCol, Color.gray, 0.5f, namePlate, baseLightCol);
            return newSkin;
        }
        public static void DeleteSkin(string name, string path)
        {
            if (Skins.ContainsKey(name))
            {
                Skins.Remove(name);
            }
            if(Panels.ContainsKey(name))
            {
                Panels.Remove(name);
            }
            if (File.Exists(path + name + ".polarskin"))
            {
                File.Delete(path + name + ".polarskin");
            }
        }
        public static void LoadSkin(string name)
        {
            SaveableSkin readData = Skins[name];
            ItePlugin.baseColor.value = readData.data.Base;
            ItePlugin.lightColor.value = readData.data.Light;
            ItePlugin.wingLightColor.value = readData.data.WingLight;
            ItePlugin.metalColor.value = readData.data.Metal;
            ItePlugin.shinyness.value = readData.data.Shinyness;
            ItePlugin.namePlate.value = readData.data.Nameplate;
            ItePlugin.namePlateColor.value = readData.data.NameplateColor;

            ItePlugin.Instance.HandleSkin();
            ItePlugin.SpawnSound(ItePlugin.mainBundle.LoadAsset<AudioClip>("SkinChange"), 1f, MonoSingleton<CameraController>.Instance.transform, 1f);
            CurrentSkin = readData;
        }
        public static void LoadAllSkins()
        {
            if (!Directory.Exists(Path))
            {
                Directory.CreateDirectory(Path);
            }
            string[] folders = Directory.GetDirectories(Path);
            string[] files = Directory.GetFiles(Path, "*.polarskin");
            foreach (string file in files)
            {
                string name = System.IO.Path.GetFileNameWithoutExtension(file);
                MakePanel(name, Path);
            }
            foreach (string folder in folders)
            {
                if(folder.Contains("Screenshots"))
                {
                    continue;
                }
                ReadFolder(folder, "");
            }
        }
        public static void ReadFolder(string folderPath, string parent)
        {
            string[] folders = Directory.GetDirectories(folderPath);
            string[] files = Directory.GetFiles(folderPath, "*.polarskin");
            MakeFolderPanel(folderPath, parent);
            foreach (string file in files)
            {
                string name = System.IO.Path.GetFileNameWithoutExtension(file);
                MakePanel(name, folderPath, parent);
            }
            foreach (string folder in folders)
            {
                if (folder.Contains("Screenshots"))
                {
                    continue;
                }
                ReadFolder(folder, MakeValidFileName(System.IO.Path.GetFileName(folderPath)));
            }
        }
        public static void SearchAndImport(string folder)
        {
            if(!Directory.Exists(folder))
            {
                ItePlugin.countOfImported.text = $"<color=red>Path doesn't exist.</color>";
                return;
            }
            int countSoFar = 0;
            string[] folders = Directory.GetDirectories(folder);
            string[] files = Directory.GetFiles(folder, "*.polarskin");
            foreach (string file in files)
            {
                if(File.Exists(file))
                {
                    countSoFar++;
                    File.Move(file, System.IO.Path.Combine(SkinImportPath, System.IO.Path.GetFileName(file)));
                }
            }
            foreach (string f in folders)
            {
                string[] sub = Directory.GetFiles(f, "*.polarskin");
                string[] subFolds = Directory.GetDirectories(f);
                foreach (string s in sub)
                {
                    if (File.Exists(s))
                    {
                        countSoFar++;
                        string folderPath = System.IO.Path.Combine(SkinImportPath, new DirectoryInfo(f).Name);
                        if(!Directory.Exists(folderPath))
                        {
                            Directory.CreateDirectory(folderPath);
                        }
                        File.Move(s, System.IO.Path.Combine(folderPath, System.IO.Path.GetFileName(s)));
                    }
                }
                if(subFolds.Length > 0)
                {
                    foreach (string s in subFolds)
                    {
                        ReadFolderII(s, ref countSoFar);
                    }
                }
            }
            ItePlugin.countOfImported.text = countSoFar > 0 ? $"Imported {countSoFar} .polarskin(s)" : $"<color=yellow>Couldn't find any .polarskin files in {new DirectoryInfo(folder).Name}</color>";
            Clear();
            LoadAllSkins();
        }
        public static void ReadFolderII(string subFold, ref int count)
        {
            string[] sub = Directory.GetFiles(subFold, "*.polarskin");
            string[] subFolds = Directory.GetDirectories(subFold);
            foreach (string s in sub)
            {
                if (File.Exists(s))
                {
                    count++;
                    string folderPath = System.IO.Path.Combine(SkinImportPath, new DirectoryInfo(subFold).Name);
                    if (!Directory.Exists(folderPath))
                    {
                        Directory.CreateDirectory(folderPath);
                    }
                    File.Move(s, System.IO.Path.Combine(folderPath, System.IO.Path.GetFileName(s)));
                }
            }
            if(subFolds.Length > 0)
            {
                foreach (string subF in subFolds)
                {
                    ReadFolderII(subF, ref count);
                }
            }
        }
    }
}
