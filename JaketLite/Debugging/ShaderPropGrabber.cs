using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace Polarite.Debugging
{
    public static class ShaderPropGrabber
    {
        public static string[] GrabProps(SkinnedMeshRenderer rend, int matIndex)
        {
            List<string> props = new List<string>();
            Shader shader = rend.materials[matIndex].shader;
            for(int i = 0; i < shader.GetPropertyCount(); i++)
            {
                props.Add(shader.GetPropertyName(i));
            }
            return props.ToArray();
        }
    }
}
