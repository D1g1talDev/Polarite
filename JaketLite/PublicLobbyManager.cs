using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using Polarite.Multiplayer;
using Steamworks;
using Steamworks.Data;
using TMPro;
using UnityEngine.UI;

namespace Polarite
{
    public static class PublicLobbyManager
    {
        public static Transform Content;

        public static GameObject SpawnLobbyObject()
        {
            GameObject lobbyObj = GameObject.Instantiate(ItePlugin.mainBundle.LoadAsset<GameObject>("PublicLobby"), Content);
            return lobbyObj;
        }
        public static void RefreshLobbies()
        {
            foreach (Transform t in Content)
            {
                GameObject.Destroy(t.gameObject);
            }
            NetworkManager.Instance.FetchPublicLobbies((Lobby? lobby) =>
            {
                if (lobby.HasValue)
                {
                    Transform lobbyObj = SpawnLobbyObject().transform;
                    lobbyObj.Find("Name").GetComponent<TextMeshProUGUI>().text = lobby.Value.GetData("LobbyName");
                    lobbyObj.Find("Difficulty").GetComponent<TextMeshProUGUI>().text = TranslateDifficulty(lobby.Value.GetData("difficulty"));
                    lobbyObj.Find("LevelName").GetComponent<TextMeshProUGUI>().text = lobby.Value.GetData("levelName");
                    Button button = lobbyObj.Find("UsefulButton").GetComponent<Button>();
                    button.onClick.AddListener(async () =>
                    {
                        await NetworkManager.Instance.JoinLobby(lobby.Value.Id);
                    });
                    bool canJoin = lobby.Value.MemberCount < lobby.Value.MaxMembers && !NetworkManager.InLobby;
                    button.interactable = canJoin;
                    lobbyObj.Find("Players").GetComponent<TextMeshProUGUI>().text = $"{lobby.Value.MemberCount}/{lobby.Value.MaxMembers}";
                }
            });
        }
        public static string TranslateDifficulty(string diff)
        {
            switch(diff)
            {
                case "0":
                    return "HARMLESS";
                case "1":
                    return "LENIENT";
                case "2":
                    return "STANDARD";
                case "3":
                    return "VIOLENT";
                case "4":
                    return "BRUTAL";
                case "5":
                    return "ULTRAKILL MUST DIE";
                case "19":
                    return "BILLION";
            }
            return "UNKNOWN";
        }
    }
}
