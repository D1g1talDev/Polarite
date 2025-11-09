using System;

using Polarite.Multiplayer;

namespace Polarite
{
    public static class CommandManager
    {
        public static readonly string[] Commands =
        {
            "level"
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
            string args = body.Length > cmd.Length
                ? body.Substring(cmd.Length).Trim()
                : string.Empty;

            switch (cmd)
            {
                case "level":
                    HandleLevel(args);
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

            if (!levelName.StartsWith("Level", StringComparison.OrdinalIgnoreCase))
            {
                levelName = $"Level {levelName.ToUpper()}";
            }
            if(NetworkManager.HostAndConnected)
            {
                SceneHelper.LoadScene(levelName);
            }
            else
            {
                NetworkManager.DisplayError("Only the host can use the level command!");
            }
        }
    }
}
