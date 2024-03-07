using Discord;
using System.Diagnostics;

namespace LinuxLauncher
{
    internal class Program
    {
        static Discord.User user = new Discord.User() { Id = 0 };
        static Discord.Discord? discord = null;
        const long DISCORD_CLIENTID = 747409309918429295;
        static string? ip;

        static void Main(string[] args)
        {
            Console.WriteLine("Experimental linux launcherk");
            Console.WriteLine("Server IP:");
            ip = Console.ReadLine();
            Console.WriteLine("Initializing Discord API..");
            InitDiscord();
        }

        static void InitDiscord()
        {
            Discord.User discord_user = new User() { Id = 0 };
            string? discord_token = null;
            try
            {
                discord = new Discord.Discord(DISCORD_CLIENTID, (uint)CreateFlags.NoRequireDiscord);
                discord.SetLogHook(LogLevel.Debug, (LogLevel level, string message) =>
                {
                    Console.WriteLine($"[Discord/{level}] {message}");
                });
                discord.RunCallbacks();
            }
            catch (ResultException result)
            {
                Console.WriteLine($"Failed to initialize Discord. {result}");
                return;
            }
            discord.GetApplicationManager().GetOAuth2Token((Result result, ref Discord.OAuth2Token token) =>
            {
                if (result != Result.Ok) return;
                discord_token = token.AccessToken;
                Console.WriteLine(discord_token);
            });
            discord.GetUserManager().OnCurrentUserUpdate += () =>
            {
                discord_user = discord.GetUserManager().GetCurrentUser();
            };

            var activity = new Discord.Activity
            {
                ApplicationId = DISCORD_CLIENTID,
                Assets = new ActivityAssets
                {
                    LargeImage = "kmp",
                    SmallImage = "karlson",
                    SmallText = "Karlson (itch.io) made by Dani"
                },
                Details = "made by devilexe",
                State = "[Closed Beta]",
                Timestamps = new ActivityTimestamps
                {
                    Start = (long)(DateTime.UtcNow - new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc)).TotalSeconds
                },
            };
            discord.GetActivityManager().UpdateActivity(activity, (_) => { });

            while (true)
            {
                if(discord_user.Id != 0 && discord_token != null)
                {
                    Process.Start("wine", $"\"./Karlson.exe\" \"linux\" \"{ip}\" \"{discord_token}\" \"{discord_user.Id}\"");
                    break;
                }

                discord.RunCallbacks();
                Thread.Sleep(100);
            }
        }
    }
}
