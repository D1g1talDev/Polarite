using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Polarite.Multiplayer;

using Steamworks;

using TMPro;

using ULTRAKILL.Cheats;

using UnityEngine;
using UnityEngine.UI;

namespace Polarite
{
    public static class PlayerList
    {
        public static Transform ContentB;

        public static void UpdatePList()
        {
            foreach (Transform t in ContentB)
            {
                GameObject.Destroy(t.gameObject);
            }

            NetworkManager.Instance.GetAllPlayersInLobby(NetworkManager.Instance.CurrentLobby, out SteamId[] ids, false);
            foreach(var p in ids)
            {
                Add(NetworkManager.GetNameOfId(p), p.Value);
            }
        }
        public static Transform Add(string name, ulong id)
        {
            Transform newList = GameObject.Instantiate(ItePlugin.mainBundle.LoadAsset<GameObject>("PlayerListing"), ContentB).transform;
            newList.Find("Name").GetComponent<TextMeshProUGUI>().text = name;
            FetchAvatar(newList.Find("PFP").GetComponent<Image>(), new Friend(id));
            Button kick = newList.Find("Kick").GetComponent<Button>();
            Button ban = newList.Find("Ban").GetComponent<Button>();
            Button steam = newList.Find("Steam").GetComponent<Button>();
            Button mute = newList.Find("Mute").GetComponent<Button>();
            mute.gameObject.SetActive(false);

            kick.interactable = NetworkManager.HostAndConnected && id != NetworkManager.Id;
            ban.interactable = NetworkManager.HostAndConnected && id != NetworkManager.Id;

            kick.onClick.AddListener(() =>
            {
                NetworkManager.Instance.KickPlayer(id);
            });
            ban.onClick.AddListener(() =>
            {
                NetworkManager.Instance.KickPlayer(id, true);
            });
            steam.onClick.AddListener(() =>
            {
                Application.OpenURL($"https://steamcommunity.com/profiles/{id}/");
            });
            if(id != NetworkManager.Id)
            {
                mute.gameObject.SetActive(true);
                TextMeshProUGUI text = mute.GetComponentInChildren<TextMeshProUGUI>();
                if (Voice.mutedPlayers.Contains(id))
                {
                    text.text = "MUTED";
                }
                else
                {
                    text.text = "UNMUTED";
                }
                mute.onClick.AddListener(() =>
                {
                    if(!Voice.idToSource.ContainsKey(id))
                    {
                        return;
                    }
                    if(Voice.mutedPlayers.Contains(id))
                    {
                        Voice.Unmute(id);
                        text.text = "UNMUTED";
                    }
                    else
                    {
                        Voice.Mute(id);
                        text.text = "MUTED";
                    }
                });
            }
            return newList;
        }
        public static async void FetchAvatar(Image target, Friend user)
        {
            Steamworks.Data.Image? image = await user.GetMediumAvatarAsync();
            if (image.HasValue)
            {
                Texture2D texture2D = new Texture2D((int)image.Value.Width, (int)image.Value.Height, TextureFormat.RGBA32, false);
                texture2D.LoadRawTextureData(image.Value.Data);
                texture2D.Apply();

                int width = (int)image.Value.Width;
                int height = (int)image.Value.Height;

                Color[] pixels = texture2D.GetPixels();
                Color[] flipped = new Color[pixels.Length];
                for (int y = 0; y < height; y++)
                {
                    Array.Copy(pixels, y * width, flipped, (height - y - 1) * width, width);
                }
                texture2D.SetPixels(flipped);
                texture2D.Apply();

                target.sprite = Sprite.Create(texture2D, new Rect(0, 0, width, height), new Vector2(0.5f, 0.5f));
                target.preserveAspect = true;
                target.SetNativeSize();
            }
        }

    }
}
