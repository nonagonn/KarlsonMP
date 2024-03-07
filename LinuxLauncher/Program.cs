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
            Process zenityInput = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = "zenity",
                    Arguments = "--entry --text \"Enter server IP\"",
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    CreateNoWindow = true
                }
            };
            zenityInput.Start();
            zenityInput.WaitForExit();
            ip = zenityInput.StandardOutput.ReadLine()?.Trim();
            if(ip == null || ip.Length < 2)
            {
                Console.WriteLine("User canceled");
                return;
            }
            Console.WriteLine(ip);
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
