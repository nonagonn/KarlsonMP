using Avalonia;
using Avalonia.Controls;
using Avalonia.Platform;
using Discord;
using System;
using System.Diagnostics;
using System.IO;
using System.Runtime.CompilerServices;
using System.Threading;

namespace KarlsonMPLauncher
{
    public partial class MainWindow : Window
    {
        string? bearer;
        long? userid;
        Discord.Discord? discord = null;
        const long DISCORD_CLIENTID = 747409309918429295;

        public MainWindow()
        {
            InitializeComponent();
        }

        private void CheckBox_Click_1(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
        {
            this.FindControl<Panel>("unsecured")!.IsVisible = this.FindControl<CheckBox>("box")!.IsChecked!.Value;
            this.FindControl<Panel>("secured")!.IsVisible = !this.FindControl<CheckBox>("box")!.IsChecked!.Value;
        }

        private void Button_Click(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
        {
            bool offline = this.FindControl<CheckBox>("box")!.IsChecked!.Value;
            string address = (string)this.FindControl<CheckBox>("address")!.Content!;
            if (offline)
            {
                string username = (string)this.FindControl<CheckBox>("username")!.Content!;
                Process.Start(Path.Combine(Directory.GetCurrentDirectory(), "Karlson.exe"), $"linux \"{address}\" \"${username}\" 0");
                return;
            }
            Process.Start(Path.Combine(Directory.GetCurrentDirectory(), "Karlson.exe"), $"\"{address}\"");
        }

        private void Button_Click_2(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
        {
            bool offline = this.FindControl<CheckBox>("box")!.IsChecked!.Value;
            string address = (string)this.FindControl<CheckBox>("address")!.Content!;
            if (offline)
            {
                string username = (string)this.FindControl<CheckBox>("username")!.Content!;
                Process.Start("wine", $"\"Karlson.exe\" linux \"{address}\" \"{username}\" 0");
                return;
            }
            Process.Start("wine", $"\"Karlson.exe\" linux \"{address}\" \"{bearer}\" {userid}");
        }

        private void Button_Click_3(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
        {
            InitDiscord();
        }

        void InitDiscord()
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
                if (discord_user.Id != 0 && discord_token != null)
                {
                    bearer = discord_token;
                    userid = discord_user.Id;
                    break;
                }

                discord.RunCallbacks();
                Thread.Sleep(100);
            }
            this.FindControl<Button>("connect_linux")!.IsVisible = true;
            this.FindControl<Button>("link_linux")!.IsVisible = false;
        }
        private void Window_Loaded_4(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
        {
            if (OperatingSystem.IsLinux())
            {
                this.FindControl<Button>("connect_windows")!.IsVisible = false;
                this.FindControl<Button>("connect_windows2")!.IsVisible = false;
            }
            else
            {
                this.FindControl<Button>("link_linux")!.IsVisible = false;
                this.FindControl<Button>("connect_linux2")!.IsVisible = false;
            }
        }
    }
}