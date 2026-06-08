using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.AddressableAssets;

namespace Polarite.Networking.Skins
{
    public struct Skin
    {
        public Color Base;
        public Color Light;
        public Color WingLight;
        public Color Metal;
        public float Shinyness;

        public ulong ID;

        public string Nameplate;
        public Color NameplateColor;
    }
    public static class MaskConsts
    {
        public const string RIGHT_ARM_MASK = "T_MainArmMask2";
        public const string FEEDBACKER_MASK = "T_FeedbackerMask4";
        public const string KNUCKLEBLASTER_MASK = "v2_armtexmask4";
        public const string WHIPLASH_MASK = "T_GreenArmMask7";
        public const string V1_BASE_MASK = "v1_mask32_nameless";
        public const string V1_WING_MASK = "v1_wingmask2_tex";
    }
    public struct CustomColorObject
    {
        public MaterialPropertyBlock Block;
        public Texture2D Mask;
        public SkinnedMeshRenderer Renderer;
        public Material Original;
        public string Name;
        public int Index;

        public CustomColorObject(SkinnedMeshRenderer rend, string maskPath, int matIndex, string name, bool lit = false)
        {
            Renderer = rend;
            Mask = ItePlugin.mainBundle.LoadAsset<Texture2D>(maskPath);
            Original = rend.materials[matIndex];
            Name = name;
            Index = matIndex;

            Block = new MaterialPropertyBlock();
            rend.materials[matIndex].shader = Addressables.LoadAssetAsync<Shader>((lit) ? "Assets/Shaders/Special/ULTRAKILL-vertexlit-customcolors.shader" : "Assets/Shaders/Main/ULTRAKILL-unlit-customcolors.shader").WaitForCompletion();

            Block.SetTexture("_IDTex", Mask);
            Block.SetTexture("_Cube", ItePlugin.mainBundle.LoadAsset<Cubemap>("cubemap"));
            Block.SetColor("_CustomColor1", Color.white);
            Block.SetColor("_CustomColor2", Color.white);
            Block.SetColor("_CustomColor3", Color.white);

            rend.SetPropertyBlock(Block, matIndex);
        }
        public void SetColor(Color col1, Color col2, Color col3, float shiny)
        {
            Color col1A = new Color(col1.r, col1.g, col1.b, shiny);
            Color col2A = new Color(col2.r, col2.g, col2.b, shiny);
            Color col3A = new Color(col3.r, col3.g, col3.b, shiny);
            Block.SetColor("_CustomColor1", col1A);
            Block.SetColor("_CustomColor2", col2A);
            Block.SetColor("_CustomColor3", col3A);
            Renderer.SetPropertyBlock(Block, Index);
        }
        public void Undo()
        {
            Renderer.materials[Index] = Original;
            if(SkinManagerV2.AffectedMeshes.Contains(Renderer))
            {
                SkinManagerV2.AffectedMeshes.Remove(Renderer);
            }
        }
    }
    public static class SkinManagerV2
    {
        public static Dictionary<string, List<CustomColorObject>> Affected = new Dictionary<string, List<CustomColorObject>>();
        public static List<SkinnedMeshRenderer> AffectedMeshes = new List<SkinnedMeshRenderer>();

        public static void CustomColor(SkinnedMeshRenderer rend, Color baseCol, Color lightCol, Color metalCol, float shinyness, string mask, string name, int targetMatIndex, bool lit = false)
        {
            CustomColorObject newObject = new CustomColorObject(rend, mask, targetMatIndex, name, lit);
            newObject.SetColor(baseCol, lightCol, metalCol, shinyness);
            if (!AffectedMeshes.Contains(rend))
            {
                AffectedMeshes.Add(rend);
            }
            AddToDict(newObject);
        }
        public static bool IsCustomShader(SkinnedMeshRenderer rend) => AffectedMeshes.Contains(rend);
        public static void Reset(string name)
        {
            if (!Affected.ContainsKey(name))
            {
                return;
            }
            foreach (var customCol in Affected[name])
            {
                customCol.Undo();
            }
            Affected.Remove(name);
        }
        public static void Clear()
        {
            Affected.Clear();
            AffectedMeshes.Clear();
        }
        public static bool ObjListContains(List<CustomColorObject> list, CustomColorObject obj)
        {
            foreach(var cObj in list)
            {
                return cObj.Renderer == obj.Renderer && cObj.Index == obj.Index;
            }
            return false;
        }
        public static void AddToDict(CustomColorObject obj)
        {
            List<CustomColorObject> newList = new List<CustomColorObject>();
            if(Affected.TryGetValue(obj.Name, out List<CustomColorObject> list))
            {
                newList.AddRange(list);
            }
            if (!ObjListContains(newList, obj))
            {
                newList.Add(obj);
            }
            if (!Affected.ContainsKey(obj.Name))
            {
                Affected.Add(obj.Name, newList);
            }
            else
            {
                Affected[obj.Name] = newList;
            }
        }
    }
}
