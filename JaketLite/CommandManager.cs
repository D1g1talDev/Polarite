using System;

using Polarite.Multiplayer;

namespace Polarite
{
    public static class CommandManager
    {
        public static readonly string[] Commands =
        {
            "level",
            "help",
            "dummy",
            "forcecomp"
        };

        public static bool IsCommand(string msg)
        {
            if (string.IsNullOrWhiteSpace(msg))
                return false;

            return msg.StartsWith("/");
        }

        public static void CheckCommand(string msg)
        {
            string body = msg.Substring(1);

            string[] parts = body.Split(' ');

            if (parts.Length == 0)
                return;

            string cmd = parts[0].ToLower();
            string args = body.Length > cmd.Length ? body.Substring(cmd.Length).Trim() : string.Empty;

            switch (cmd)
            {
                case "level":
                    HandleLevel(args);
                    break;
                case "help":
                    HelpCommand();
                    break;
                case "dummy":
                    DummyMode();
                    break;
                case "forcecomp":
                    if(NetworkManager.InLobby)
                    {
                        // ensure you get no rank at all
                        StatsManager.Instance.kills = 0;
                        StatsManager.Instance.challengeComplete = false;
                        StatsManager.Instance.stylePoints = 0;
                        StatsManager.Instance.rankScore = 0;
                        SceneHelper.SpawnFinalPitAndFinish();
                    }
                    break;
            }
        }

        private static void HandleLevel(string args)
        {
            if (string.IsNullOrWhiteSpace(args))
            {
                NetworkManager.DisplaySystemChatMessage("Level command usage: /level <level>");
                return;
            }

            string levelName = args;
            string overriddeLevelName = string.Empty;

            if(args == "cybergrind")
            {
                overriddeLevelName = "Endless";
            }
            if(args == "credits")
            {
                overriddeLevelName = "CreditsMuseum2";
            }
            if(args == "sandbox")
            {
                overriddeLevelName = "uk_construct";
            }

            if (!levelName.StartsWith("Level", StringComparison.OrdinalIgnoreCase) && string.IsNullOrEmpty(overriddeLevelName))
            {
                levelName = $"Level {levelName.ToUpper()}";
            }
            else if (!string.IsNullOrEmpty(overriddeLevelName))
            {
                levelName = overriddeLevelName;
            }
            if (NetworkManager.HostAndConnected)
            {
                SceneHelper.LoadScene(levelName);
            }
            else
            {
                NetworkManager.DisplayError("Only the host can use the level command!");
            }
        }
        private static void HelpCommand()
        {
            NetworkManager.DisplaySystemChatMessage("Commands:\n<b>/level <level name></b>\nShortcuts:\n\n<b>cybergrind\ncredits\nsandbox</b>\nExample:\nlevel p-1\n\n<b>/forcecomp</b>\n\nBeats the level with a D rank");
        }
        // adding commands just to add commands atp
        private static void DummyMode()
        {
            foreach(var p in NetworkManager.players.Values)
            {
                p.NameTag.dummy = !p.NameTag.dummy;
            }
            NetworkManager.DisplayGameChatMessage("Have fun seeing everyone as just a worthless dummy");
        }
    }
}
