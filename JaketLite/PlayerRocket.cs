using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Polarite.Multiplayer;

using TMPro;

using UnityEngine;

namespace Polarite
{
    public class PlayerRocket : MonoBehaviour
    {
        public ulong owner;
        public bool frozen;
        public Grenade rocket;
        public TextMeshPro text;

        void Start()
        {
            rocket = GetComponent<Grenade>();
            GameObject nameOfOwner = new GameObject("Name", typeof(TextMeshPro));
            nameOfOwner.transform.SetParent(rocket.transform, true);
            TextMeshPro t = nameOfOwner.GetComponent<TextMeshPro>();
            t.text = $"<color=#42ddf5>{NetworkManager.GetNameOfId(owner)}</color>";
            t.fontSize = 7.5f;
            t.font = OptionsManager.Instance.optionsMenu.transform.GetComponentInChildren<TextMeshProUGUI>().font;
            rocket.freezeEffect.transform.localScale /= 1.5f;
            text = t;
        }
        
        void Update()
        {
            text.gameObject.SetActive(frozen);
            text.transform.position = new Vector3(rocket.transform.position.x, rocket.transform.position.y + 1.25f, rocket.transform.position.z);

            Transform cam = Camera.current.transform;
            Vector3 dir = (text.transform.position - cam.position).normalized;
            text.transform.rotation = Quaternion.LookRotation(dir, Vector3.up);
        }

    }
}
