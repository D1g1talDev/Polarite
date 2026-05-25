using Polarite.Multiplayer;
using Polarite.Networking.Skins;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace Polarite
{
    public class Ragdoll : MonoBehaviour
    {
        public SkinnedMeshRenderer renderer;
        public Skin target;
        public ulong targetId;

        public void SetValues(Skin skin, ulong id)
        {
            target = skin;
            targetId = id;
        }
        public void Start()
        {
            renderer = GetComponentInChildren<SkinnedMeshRenderer>();
            if(renderer != null)
            {
                NetworkPlayer.SetSkinOfRagdoll(renderer, target, targetId);
            }
        }
    }
}
