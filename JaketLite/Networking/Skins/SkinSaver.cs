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

        public SkinPanel(SaveableSkin save)
        {
            save.name = SkinSaver.MakeValidFileName(save.name);
            int randomSuffix = UnityEngine.Random.Range(0, int.MaxValue);
            panel = new ConfigPanel(ItePlugin.savePanel, save.name, $"skin.{save.name}.{randomSuffix}");
            loadHeader = new ConfigHeader(panel, "<color=red>This will <b>overwrite</b> your <b>custom skin configuration!</b></color>");
            loadButton = new ButtonField(panel, "Load", panel.guid + ".load." + randomSuffix);
            deleteButton = new ButtonField(panel, "Delete", panel.guid + ".del." + randomSuffix);

            loadButton.onClick += () => SkinSaver.LoadSkin(save.name);
            deleteButton.onClick += () =>
            {
                panel.hidden = true;
                SkinSaver.DeleteSkin(save.name);
            };
            this.save = save;
        }
    }
    public static class SkinSaver
    {
        public static string Path = Application.persistentDataPath + "/Skins/";
        public static Dictionary<string, SaveableSkin> Skins = new Dictionary<string, SaveableSkin>();
        public static Dictionary<string, SkinPanel> Panels = new Dictionary<string, SkinPanel>();

        public static SaveableSkin CurrentSkin;

        public static void Clear()
        {
            Skins.Clear();
            foreach(var pan in Panels.Values)
            {
                pan.panel.hidden = true;
            }
            Panels.Clear();
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
        }
        public static void MakePanel(string name)
        {
            string validName = MakeValidFileName(name);
            if (!Panels.ContainsKey(validName))
            {
                SaveableSkin readData = ReadSkin(validName);
                // incase it's a safe skin
                validName = MakeValidFileName(readData.name);
                SkinPanel panel = new SkinPanel(readData);
                Panels[validName] = panel;
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
            File.WriteAllBytes(Path + validName + ".polarskin", writer.GetBytes());
            if(!secret)
            {
                MakePanel(validName);
                Application.OpenURL(Path);
            }
        }
        public static SaveableSkin ReadSkin(string name)
        {
            if (File.Exists(Path + name + ".polarskin"))
            {
                try
                {
                    byte[] data = File.ReadAllBytes(Path + name + ".polarskin");
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
                    byte[] data = File.ReadAllBytes(Path + name + ".polarskin");
                    string newName = name + " (Fixed)";
                    SaveableSkin skin = new SaveableSkin()
                    {
                        name = newName,
                        data = GetSafeSkin(data, name)
                    };
                    Skins[newName] = skin;
                    Logs.Warn($"Detected potentially outdated skin {name}, The skin has been remade.");
                    DeleteSkin(name);
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
        public static void DeleteSkin(string name)
        {
            if (Skins.ContainsKey(name))
            {
                Skins.Remove(name);
            }
            if(Panels.ContainsKey(name))
            {
                Panels.Remove(name);
            }
            if (File.Exists(Path + name + ".polarskin"))
            {
                File.Delete(Path + name + ".polarskin");
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
            string[] files = Directory.GetFiles(Path, "*.polarskin");
            foreach (string file in files)
            {
                string name = System.IO.Path.GetFileNameWithoutExtension(file);
                MakePanel(name);
            }
        }
    }
}
