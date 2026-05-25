using Polarite.Networking.Skins;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace Polarite
{
    public static class MasterShaderizer
    {
        public static void MasterShaderize(SkinnedMeshRenderer rend)
        {
            if (rend != null && !SkinManagerV2.IsCustomShader(rend))
            {
                foreach(var mat in rend.materials)
                {
                    mat.shader = DefaultReferenceManager.Instance.masterShader;
                    // make textures not look dark
                    if(IsVertex(mat))
                    {
                        mat.DisableKeyword("VERTEX_LIGHTING");
                    }
                }
            }
        }
        public static void MasterShaderize(Renderer rend)
        {
            if (rend != null)
            {
                foreach (var mat in rend.materials)
                {
                    mat.shader = DefaultReferenceManager.Instance.masterShader;
                    // make textures not look dark
                    if (IsVertex(mat))
                    {
                        mat.DisableKeyword("VERTEX_LIGHTING");
                    }
                }
            }
        }
        public static bool IsVertex(Material mat)
        {
            foreach(var str in mat.shaderKeywords)
            {
                return str == "VERTEX_LIGHTING";
            }
            return false;
        }
    }
}
