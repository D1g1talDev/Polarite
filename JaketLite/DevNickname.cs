using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace Polarite
{
    public static class Nickname
    {
        public static string[] Words = new string[]
        {
            "Shell",
            "Clique",
            "News",
            "Decrease",
            "Plug",
            "Revoke",
            "Poetry",
            "Machinery",
            "Equal",
            "Detective",
            "Century",
            "Neck",
            "Professional",
            "Harbor",
            "Egg",
            "Nuance",
            "Calculation",
            "Gold",
            "Spontaneous",
            "Winner",
            "Image",
            "Tolerant",
            "Portrait",
            "Constitutional",
            "Ear",
            "Cord",
            "Density",
            "Beat",
            "Retailer",
            "Belief",
            "Clean",
            "Wrist",
        };
        public static string Get()
        {
            bool addNumbers = Random.value > 0.5f;
            if(addNumbers)
                return $"{Words[Random.Range(0, Words.Length)]}{Words[Random.Range(0, Words.Length)]}{Random.Range(0, 1000)}";
            else
                return $"{Words[Random.Range(0, Words.Length)]}{Words[Random.Range(0, Words.Length)]}";
        }
    }
}
