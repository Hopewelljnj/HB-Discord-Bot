﻿using Discord;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using Discord_Bot.Commands;
using Discord_Bot.Games;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Net.Http;
using System.Collections.Specialized;
using System.Net;
using System.Web;
using System.Diagnostics;
using System.IO;
using System.Linq;

namespace Discord_Bot
{
    class Program
    {
        private static DiscordClient _client;
        private static CommandsPlugin _commands, _admincommands;
        private static GamePlugin _gamePlugin;

        private static List<User> Admins = new List<User>();
        private static List<User> SuperAdmins = new List<User>();
        private static Dictionary<string, UserInfo> userInfo = new Dictionary<string, UserInfo>();
        private static Server Commons;

        private static Role OwnerRole;
        private static Role DemiGodRole;
        private static Role RoyalGuardRole;
        private static Role CommanderRole;
        private static Role TrustedRole;
        private static Role StandardRole;

        static void Main(string[] args)
        {

            var client = new DiscordClient();

            _client = client;
            _client.LogMessage += (s, e) => Console.WriteLine($"[{e.Severity}] {e.Source}: {e.Message}");

            if (File.Exists("../UserInfo.json"))
            {
                var sw = new StreamReader("../UserInfo.json");

                string json = sw.ReadToEnd();
                userInfo = JsonConvert.DeserializeObject<Dictionary<string, UserInfo>>(json);
                sw.Close();
            }

            _commands = new CommandsPlugin(client);
            _admincommands = new CommandsPlugin(client, GetPermissions);
            _commands.CreateCommandGroup("", group => BuildCommands(group));
            _admincommands.CreateCommandGroup("admin", adminGroup => BuildAdminCommands(adminGroup));

            _gamePlugin = new GamePlugin(client);

            _client.UserAdded += async (s, e) =>
            {
                Server Commons = _client.GetServer("99333280020566016");
                var standardRole = _client.FindRoles(Commons, "Standard");

                if (e.Server != Commons)
                    return;

                await _client.SendMessage(_client.GetChannel("99341276532449280"), $"Holy shit! A new user! Welcome {Mention.User(e.User)}");
                await NewUserText(e.User);
                if(e.User.Roles.Count() == 1)
                    await _client.EditUser(e.User, null, null, standardRole);
            };

            _client.Disconnected += async (s, e) =>
            {
                while(_client.State != DiscordClientState.Connected)
                {
                    await _client.Connect(".li", "");

                    await Task.Delay(30000);
                }
            };

            _commands.CommandError += async (s, e) =>
            {
                var ex = e.Exception.GetBaseException();
                if (ex is PermissionException)
                    await Reply(e, "Sorry, you do not have the permissions to use this command!");
                else if (ex is TimeException)
                    await Task.Delay(1);
                else
                    await Reply(e, $"Error: {ex.Message}.");

            };
            
            _client.Run(async () =>
            {
                await _client.Connect("hidden :)", "hidden :)");

                Commons = _client.GetServer("99333280020566016");

                OwnerRole = _client.GetRole("103573196468404224");
                DemiGodRole = _client.GetRole("104390778037481472");
                RoyalGuardRole = _client.GetRole("106584302090792960");
                CommanderRole = _client.GetRole("99333495347748864");
                TrustedRole = _client.GetRole("99834853003886592");
                StandardRole = _client.GetRole("99656622212661248");

                Role[] rolesToAdd = { StandardRole };
                
                foreach (var god in OwnerRole.Members)
                {
                    Admins.Add(god);
                    SuperAdmins.Add(god);
                }

                foreach (var demiGod in DemiGodRole.Members)
                {
                    Admins.Add(demiGod);
                    SuperAdmins.Add(demiGod);
                }

                foreach (var royalGuard in RoyalGuardRole.Members)
                {
                    Admins.Add(royalGuard);
                    SuperAdmins.Add(royalGuard);

                }

                foreach (var commander in CommanderRole.Members)
                    Admins.Add(commander);
            });
        }

        private static int GetPermissions(User u)
        {
            if (isAdmin(u))
                return 10;
            else
                return 0;
        }

        protected static async Task NewUserText(User e)
        {
            StreamReader streamReader = new StreamReader("../BeginnersText.txt");

            string GettingStarted = await streamReader.ReadToEndAsync();
            GettingStarted = String.Format(GettingStarted, Mention.User(e), Mention.User(_client.GetUser(Commons, "83677331951976448")));
            streamReader.Close();
            string[] reply = GettingStarted.Split(new string[] { "[SPLIT]" }, StringSplitOptions.None);
            foreach (var message in reply)
                await _client.SendPrivateMessage(e, message);
            
        }

        private static Random random = new Random();
        private static void BuildCommands(CommandGroupBuilder group)
        {
            group.DefaultMinPermissions(0);

            group.CreateCommand("img")
                .WithPurpose("Get a random image pulled from Google!")
                .AnyArgs()
                .IsHidden()
                .Do(async e =>
                {
                    var client = new HttpClient();
                    string uri = $"https://ajax.googleapis.com/ajax/services/search/images?";
                    NameValueCollection values = HttpUtility.ParseQueryString(string.Empty);
                    values.Add("v", "1.0");
                    values.Add("q", e.ArgText);
                    values.Add("rsz", "8");
                    values.Add("start", random.Next(1, 12).ToString());
                    values.Add("safe", "active");



                    try
                    {
                        var response = await Get(uri, values);
                        var data = JObject.Parse(response);
                        List<string> images = new List<string>();
                        foreach (var element in data["responseData"]["results"])
                        {
                            var image = element["unescapedUrl"];
                            images.Add(image.ToString());
                        }

                        var imageURL = images[random.Next(images.Count)].ToString();
                        Console.WriteLine(imageURL);
                        await Reply(e, $"{e.ArgText} : {imageURL}");
                    }
                    catch (Exception ex)
                    {
                        await Reply(e, $"{ex.Message}");
                    }
                });
            group.CreateCommand("hb")
                .WithPurpose("Find a User's HummingBird account with it's information!")
                .ArgsAtMax(1)
                .IsHidden()
                .Do(async e =>
                {
                    var client = new HttpClient();
                    string url = $"http://hummingbird.me/api/v1/users/{e.ArgText}";
                    string userUrl = $"http://hummingbird.me/users/{e.ArgText}";

                    try
                    {
                        string response = await client.GetStringAsync(url);
                        var json = JObject.Parse(response);

                        var username = json["name"].ToString();
                        var waifu = json["waifu"].ToString();
                        var waifu_prefix = json["waifu_or_husbando"].ToString();
                        var avatar = json["avatar"].ToString();
                        var about = json["about"].ToString();
                        var bio = json["bio"].ToString();
                        var location = json["location"].ToString();
                        var website = json["website"].ToString();
                        var life_spent_on_anime = Int32.Parse(json["life_spent_on_anime"].ToString());

                        string lifeAnime = CalculateTime(life_spent_on_anime);

                        string messageToPost = $@"
**User**: {username}
**Avatar**: {avatar} 
**{waifu_prefix}**: {waifu}
**Bio:** {bio}
**Time wasted on Anime:** {lifeAnime}";

                        if (String.IsNullOrWhiteSpace(location))
                            messageToPost += $"\n**Location:** {location}";
                        if (String.IsNullOrWhiteSpace(website))
                            messageToPost += $"\n**Website:** {website}";

                        messageToPost += $"\n{userUrl}";

                        await Reply(e, messageToPost);

                    }
                    catch (Exception ex)
                    {
                        await Reply(e, $"Error: {ex.Message}");
                    }

                });
            group.CreateCommand("8ball")
                .WithPurpose("The magic eightball will answer all your doubts and questions!")
                .AnyArgs()
                .MinuteDelay(1)
                .Do(async e =>
                {
                    string[] responses = { "Not so sure", "Most likely", "Absolutely not", "Outlook is good", "Never",
"Negative", "Could be", "Unclear, ask again", "Yes", "No", "Possible, but not probable" };
                    string response;


                    if (e.ArgText.Length == 0)
                        response = "I can't do anything with empty prompts.";
                    else if (e.ArgText[e.ArgText.Length - 1] != '?')
                        response = "Please end your sentence with a question mark appropriately.";
                    else
                        response = responses[random.Next(responses.Length)];
                    
                    await Reply(e, response);
                });
            group.CreateCommand("ayy")
                .HourDelay(1)
                .AnyArgs()
                .Do(async e =>
                {
                    await Reply(e, "ayy", false);
                });
            group.CreateCommand("lmao")
                .AnyArgs()
                .HourDelay(1)
                .Do(async e =>
                {
                    await Reply(e, "https://www.youtube.com/watch?v=HTLZjhHIEdw");
                });
            group.CreateCommand("no")
                .SecondDelay(120)
                .AnyArgs()
                .Do(async e =>
                {
                    await Reply(e, "pignig", false);
                });
            group.CreateCommand("hello")
                .AnyArgs()
                .HourDelay(1)
                .Do(async e =>
                {   
                    await Reply(e, $"Hello, {Mention.User(e.User)}", false);
                });
            group.CreateCommand("bullying")
                .AnyArgs()
                .WithPurpose("Getting bullied?")
                .IsHidden()
                .MinuteDelay(30)
                .Do(async e =>
                {
                    var OnlineAdmins = new List<User>();

                    foreach (var admin in Admins)
                        if (admin.Status == UserStatus.Online || admin.Status == "idle")
                            OnlineAdmins.Add(admin);

                    User toMention;
                    if (OnlineAdmins.Count != 0)
                        toMention = OnlineAdmins[random.Next(OnlineAdmins.Count)];
                    else
                        toMention = Admins[random.Next(Admins.Count)];

                    await _client.SendFile(e.Channel, "antibully.jpg");
                    await Reply(e, $"{Mention.User(toMention)} **BULLYING IN PROGESS :: {e.User.Name.ToUpper()} IS BEING BULLIED** ", false);
                    await Task.Delay(300);
                    await Reply(e, $"{Mention.User(toMention)} **BULLYING IN PROGESS :: {e.User.Name.ToUpper()} IS BEING BULLIED** ", false);
                });
            group.CreateCommand("commands")
                .AnyArgs()
                .IsHidden()
                .Do(async e =>
                {
                    string response = $"The character to use a command right now is '{_commands.CommandChar}'.\n";
                    foreach(var cmd in _commands._commands)
                    {
                        if(!String.IsNullOrWhiteSpace(cmd.Purpose))
                        {
                            response += $"**{cmd.Parts[0]}** - {cmd.Purpose}";

                            if (cmd.CommandDelay == null)
                                response += "\n";
                            else
                                response += $" **|** Time limit: once per {cmd.CommandDelayNotify} {cmd.timeType}.\n";
                        }
                    }
                    
                    await _client.SendPrivateMessage(e.User, response);
                });
            group.CreateCommand("help")
                .WithPurpose("Show the getting-started guide!")
                .AnyArgs()
                .IsHidden()
                .Do(async e =>
                {
                    await NewUserText(e.User);
                });
            group.CreateCommand("feedback")
                .WithPurpose("Give feedback to the bot! Stepper will read it sometime soon.. I think.")
                .ArgsAtLeast(1)
                .IsHidden()
                .Do(async e =>
                {
                    StreamWriter fs = new StreamWriter("../feedback.txt", true);
                    await fs.WriteLineAsync($"{e.User.Name} suggested: {e.ArgText}");
                    fs.Close();
                });
            group.CreateCommand("roulette")
                .AnyArgs()
                .MinuteDelay(2)
                .WithPurpose("have a 50% procent chance of timing yourself out.")
                .Do(async e =>
                {
                    if (e.Channel.IsPrivate)
                        return;

                    int chance = random.Next(0, 100);

                    try
                    {
                        if (chance > 50)
                            await Reply(e, "has not been timed out! Hooray!");
                        else
                            await Timeout(e, 2);
                    }
                    catch (Exception ex)
                    {
                        await Reply(e, ex.Message);
                    }

                });
            group.CreateCommand("seppuku")
                .MinuteDelay(2)
                .IsHidden()
                .AnyArgs()
                .WithPurpose("Time yourself out for 2 minutes.")
                .Do(async e =>
                {
                    if (e.Channel.IsPrivate)
                        return;
                        
                    try
                    {
                        await Timeout(e, 2);
                    }
                    catch(Exception ex)
                    {
                        await Reply(e, ex.Message);
                    }
                    
                });
            //group.CreateCommand("")
            //    .ArgsAtMax(1)
            //    .IsHidden()
            //    .Do(async e =>
            //    {
            //        User playingUser = GetUser(e.Server, e.Args[0]);
            //    });
        }

        private static void BuildAdminCommands(CommandGroupBuilder adminGroup)
        {
            adminGroup.DefaultMinPermissions(3);

            adminGroup.CreateCommand("delete")
                .WithPurpose("Delete messages on this channel. Usage: `/admin delete {number of messages to delete}`.")
                .ArgsEqual(1)
                .Do(async e =>
                {
                    if (!isAdmin(e.User))
                        return;

                    if (e.Channel.IsPrivate)
                        return;

                    int deleteNumber = 0;

                    Int32.TryParse(e.Args[0], out deleteNumber);

                    var messages = await _client.DownloadMessages(e.Channel, deleteNumber + 1);

                    await _client.DeleteMessages(messages);
                    await Reply(e, $"just deleted {deleteNumber} messages on this channel!");
                });
            adminGroup.CreateCommand("kick")
                .WithPurpose("Only for super admins! Usage: `/admin kick {@username}`")
                .ArgsEqual(1)
                .Do(async e =>
                {
                    if (!isSuperAdmin(e.User))
                        return;

                    if (e.Channel.IsPrivate)
                        return;

                    User userToKick = GetUser(e.Server, e.Args[0]);

                    if (userToKick == null)
                        return;

                    if (userToKick == _client.CurrentUser)
                        return;

                    await _client.SendPrivateMessage(userToKick, $"You've been kicked by {e.User.Name}, you can rejoin by using this url: https://discord.gg/0YOrPxx9u1wtJE0B");
                    await Reply(e, $"just kicked {userToKick.Name}!");
                    await _client.KickUser(userToKick);
                });
            adminGroup.CreateCommand("timeout")
                .WithPurpose("Time out someone. Usage: `/admin timeout {@username} {time in minutes}`.")
                .ArgsAtLeast(1)
                .Do(async e =>
                {
                    if (e.Args.Count() < 2)
                    {
                        await Reply(e, "command was not in the right format. Usage: `/admin timeout {username} {time in minutes}`");
                        return;
                    }

                    if (e.Channel.IsPrivate)
                        return;

                    if (!isAdmin(e.User))
                        return;

                    User userToTimeOut = GetUser(e.Server, e.Args[0]);

                    if (userToTimeOut == null || userToTimeOut == _client.CurrentUser)
                    {
                        await Reply(e, "Couldn't find user.");
                        return;
                    }


                    //If the user is a super admin or if the user is a commander trying to kick another commander. Stop
                    if (isSuperAdmin(userToTimeOut) || (e.User.HasRole(CommanderRole) && userToTimeOut.HasRole(CommanderRole)))
                    {
                        await Reply(e, $"{Mention.User(userToTimeOut)} cannot be timed out.");
                        return;
                    }

                    int minutes = 0;
                    try
                    {
                        minutes = Int32.Parse(e.Args[1]);
                    }
                    catch (FormatException)
                    {
                        await Reply(e, "command was not in the right format. Usage: `/admin timeout {username} {time in minutes}`");
                        return;
                    }

                    await Timeout(e, userToTimeOut, minutes);
                });
            adminGroup.CreateCommand("commands")
                .IsHidden()
                .AnyArgs()
                .Do(async e =>
                {
                    string response = $"The character to use a command right now is '{_commands.CommandChar}'.\n";
                    foreach (var cmd in _admincommands._commands)
                    {
                        if (!String.IsNullOrWhiteSpace(cmd.Purpose))
                        {
                            string command = "";
                            foreach (var cmdPart in cmd.Parts)
                                command += cmdPart + ' ';

                            response += $"**{command}** - {cmd.Purpose}";

                            if (cmd.CommandDelay == null)
                                response += "\n";
                            else
                                response += $" **|** Time limit: once per {cmd.CommandDelayNotify} {cmd.timeType}.\n";
                        }
                    }

                    await _client.SendPrivateMessage(e.User, response);
                });
        }

        protected static async Task Timeout(CommandArgs e, int minutes)
        {
            string postfix = "minute";
            if (minutes > 1)
                postfix += 's';

            UserInfo u = null;

            //Timeout caluclator
            if (!userInfo.ContainsKey(e.User.Id))
                userInfo.Add(e.User.Id, new UserInfo());

            u = userInfo[e.User.Id];
            u.TimoutTotalTime += minutes;
            u.TimeoutNumber += 1;

            var UserRoles = e.User.Roles;

            await _client.EditUser(e.User, null, null, new Role[] { Commons.EveryoneRole });
            await Reply(e, $"has been timed out for {minutes} {postfix}! He's now been timed out a total of {u.TimeoutNumber} times and a total of {u.TimoutTotalTime} minutes!");
            string json = JsonConvert.SerializeObject(userInfo);
            StreamWriter sw = new StreamWriter("../UserInfo.json", false);
            await sw.WriteAsync(json);
            sw.Close();
            await Task.Delay(minutes * 60000);
            await _client.EditUser(e.User, false, false, UserRoles);
        }

        protected static async Task Timeout(CommandArgs e, User user, int minutes)
        {
            var UserRoles = user.Roles;
            await _client.EditUser(user, null, null, new Role[] { Commons.EveryoneRole });
            await Reply(e, $"has timed out {Mention.User(user)} for {minutes} minutes.");
            await Task.Delay(minutes * 60000);
            await _client.EditUser(user, false, false, UserRoles);
        }

        protected static User GetUser(Server server, string userName)
        {
            string userID = userName;
            if(userName[0] == '@')
                 userID = userName.Substring(1);

            var Users = _client.FindUsers(server, userID);
            var user = Users.FirstOrDefault();

            return user;
        }

        protected static bool isSuperAdmin(User user)
        {
            bool canDelete = false;

            foreach (var admin in SuperAdmins)
            {
                if (user == admin)
                {
                    canDelete = true;
                    break;
                }
            }

            return canDelete;
        }
        
        protected static bool isAdmin(User user)
        {
            bool canDelete = false;

            foreach (var admin in Admins)
            {
                if (user == admin)
                {
                    canDelete = true;
                    break;
                }
            }

            return canDelete;
        }

        protected static string CalculateTime(int minutes)
        {
            if (minutes == 0)
                return "No time.";

            int years, months, days, hours = 0;

            hours = minutes / 60;
            minutes %= 60;
            days = hours / 24;
            hours %= 24;
            months = days / 30;
            days %= 30;
            years = months / 12;
            months %= 12;

            string animeWatched = "";

            if(years > 0)
            {
                animeWatched += years;
                if (years == 1)
                    animeWatched += " **year**";
                else
                    animeWatched += " **years**";
            }

            if(months > 0)
            {
                if (animeWatched.Length > 0)
                    animeWatched += ", ";
                animeWatched += months;
                if (months == 1)
                    animeWatched += " **month**";
                else
                    animeWatched += " **months**";
            }

            if(days > 0)
            {
                if (animeWatched.Length > 0)
                    animeWatched += ", ";
                animeWatched += days;
                if (days == 1)
                    animeWatched += " **day**";
                else
                    animeWatched += " **days**";
            }

            if(hours > 0)
            {
                if (animeWatched.Length > 0)
                    animeWatched += ", ";
                animeWatched += hours;
                if (hours == 1)
                    animeWatched += " **hour**";
                else
                    animeWatched += " **hours**";
            }

            if(minutes > 0)
            {
                if (animeWatched.Length > 0)
                    animeWatched += " and ";
                animeWatched += minutes;
                if (minutes == 1)
                    animeWatched += " **minute**";
                else
                    animeWatched += " **minutes**";
            }

            return animeWatched;


        }

        protected static async Task<string> Get(string url, NameValueCollection values)
        {
            string hello = String.Empty;
            var client = new HttpClient();
            url += values.ToString();
            hello = await client.GetStringAsync(url);
            return hello;
        }



        protected static async Task<string> Post(string Uri, Dictionary<string, string> values)
        {
            string response;
            var client = new HttpClient();
            var content = new FormUrlEncodedContent(values);
            var request = await client.PostAsync(Uri, content);
            response = request.ToString();
            return response;

        }

        protected static async Task Reply(User user, Channel channel, string text, bool mentionUser)
        {
            if (!string.IsNullOrWhiteSpace(text))
            {
                if (channel.IsPrivate || !mentionUser)
                    await _client.SendMessage(channel, text);
                else
                    await _client.SendMessage(channel, $"{Mention.User(user)}: {text}");
            }
        }
        protected static Task Reply(CommandArgs e, string text)
            => Reply(e.User, e.Channel, text, true);
        protected static Task Reply(CommandArgs e, string text, bool mentionUser)
            => Reply(e.User, e.Channel, text, mentionUser);

    }
}
