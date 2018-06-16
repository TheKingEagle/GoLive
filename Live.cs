using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Discord.WebSocket;
using Discord.Commands;
using Discord;
using System.IO;
using Newtonsoft.Json;
using RMSoftware.ModularBot;
namespace GoLive
{
    public class TwitchNotificationService
    {
        DiscordSocketClient _client = null;
        List<UserShout> shoutEntities = new List<UserShout>();
        public ConsoleLogWriter _writer { get; set; }
        public bool Initialized = false;
        public TwitchNotificationService(DiscordSocketClient client, ConsoleLogWriter writer)
        {
            if(writer == null)
            {
                Console.WriteLine("WRITER IS NULL!");
            }
            if (client == null)
            {
                Console.WriteLine("CLIENT IS NULL!");
            }
            
            _client = client;
            _writer = writer;
            
            _client.GuildMemberUpdated += _client_GuildMemberUpdated;
            LogMessage l2 = new LogMessage(LogSeverity.Info, "GoLive", "TwitchNotificationService constructor called.");
            Client_Log(l2);
            //Read shouts.json, otherwise create it.
            using (FileStream fs = File.Open("shouts.json", FileMode.OpenOrCreate))
            {
                using (StreamReader sr = new StreamReader(fs))
                {
                    string f = sr.ReadToEnd();
                    if (!string.IsNullOrWhiteSpace(f))
                    {
                        JsonSerializerSettings s = new JsonSerializerSettings();
                        s.Formatting = Formatting.Indented;
                        s.ConstructorHandling = ConstructorHandling.AllowNonPublicDefaultConstructor;
                        s.ReferenceLoopHandling = ReferenceLoopHandling.Serialize;
                        s.TypeNameHandling = TypeNameHandling.Arrays;
                        shoutEntities = ((Newtonsoft.Json.Linq.JArray)JsonConvert.DeserializeObject(f,s)).ToObject<List<UserShout>>();
                        if (shoutEntities == null)
                        {
                            shoutEntities = new List<UserShout>();
                            LogMessage l = new LogMessage(LogSeverity.Warning, "GoLive", "WARNING: Shouts.json is null or invalid.");
                            Client_Log(l);
                        }
                    }
                }
            }
        }
        private Task Client_Log(LogMessage arg)
        {
            _writer.WriteEntry(arg,ConsoleColor.DarkMagenta);
            return Task.Delay(0);
        }
        /// <summary>
        /// Service. Add a new shout entity.
        /// </summary>
        /// <param name="context"></param>
        /// <param name="user"></param>
        /// <param name="SupressEveryone"></param>
        /// <returns></returns>
        public async Task AddShoutEntity(ICommandContext context, SocketGuildUser user, bool SupressEveryone)
        {
            UserShout item = shoutEntities.FirstOrDefault(x => x.userID == user.Id);
            if (item != null)
            {
                //That user is already in the list. Cool! Check the channel and alter nothing else.
                
                if(item.ChannelIDs.Contains(context.Channel.Id))
                {
                    await context.Channel.SendMessageAsync("This channel already has a notification for this user. Please use a different channel.\r\n"+
                        "**Please note: You cannot add multiple twitch accounts to the same user.**");
                    return;//Cancel the operation. NO SAVE.
                }
                else
                {
                    item.AddChannel(context.Channel.Id);//An existing channelID wasn't found. Add it to the list.
                    await context.Channel.SendMessageAsync($"This channel is now subscribed to going live alerts from (Discord user: {user.Username})");
                }
            }
            else
            {
                shoutEntities.Add(new UserShout(user.Id, context.Channel.Id, SupressEveryone));
                //Add a brand new item to our shout list.
                await context.Channel.SendMessageAsync($"This channel is now subscribed to going live alerts from (Discord user: {user.Username})");
            }
            //Save shouts.json.
            using (FileStream fs = File.Create("shouts.json"))
            {
                using (StreamWriter sw = new StreamWriter(fs))
                {
                    sw.WriteLine(JsonConvert.SerializeObject(shoutEntities,Formatting.Indented));//Output and save a file.
                    sw.Flush();
                }

            }
        }

        /// <summary>
        /// Service. Removes an entire user's stream alerts.
        /// </summary>
        /// <param name="context"></param>
        /// <param name="user"></param>
        /// <returns></returns>
        public async Task RemoveShoutEntity(ICommandContext context, SocketGuildUser user)
        {
            UserShout item = shoutEntities.FirstOrDefault(x => x.userID == user.Id);
            if (item != null)
            {
                //That user is already in the list. Cool! Check the channel and alter nothing else.

                shoutEntities.Remove(item);
                await context.Channel.SendMessageAsync($"Removed {(await context.Guild.GetUserAsync(item.userID)).Username}'s stream alert.");
            }
            else
            {
                await context.Channel.SendMessageAsync($"Hey! This user doesn't have any stream alerts. Try that again please.");
                return;
            }
            //Save shouts.json.
            using (FileStream fs = File.Create("shouts.json"))
            {
                using (StreamWriter sw = new StreamWriter(fs))
                {
                    sw.WriteLine(JsonConvert.SerializeObject(shoutEntities, Formatting.Indented));//Output and save a file.
                    sw.Flush();
                }

            }
        }

        /// <summary>
        /// Service. Remove a channel from a user's stream alert. This must be called from the channel where the alert is set.
        /// </summary>
        /// <param name="context"></param>
        /// <param name="user"></param>
        /// <returns></returns>
        public async Task RemoveEntityChannel(ICommandContext context, SocketGuildUser user)
        {
            UserShout item = shoutEntities.FirstOrDefault(x => x.userID == user.Id);

            SocketTextChannel channel = context.Channel as SocketTextChannel;
            if (item != null)
            {
                //That user is already in the list. Cool! Check the channel and alter nothing else.

                ulong chToRem = item.ChannelIDs.FirstOrDefault(x => x == channel.Id);
                if(chToRem == 0)
                {
                    await context.Channel.SendMessageAsync($"A notification for {(await context.Guild.GetUserAsync(item.userID)).Username}'s stream alert does not exist for this channel.");
                    return;
                }
                item.ChannelIDs.Remove(chToRem);
                await context.Channel.SendMessageAsync($"Removed {(await context.Guild.GetUserAsync(item.userID)).Username}'s stream alert. from this channel.");
            }
            else
            {
                await context.Channel.SendMessageAsync($"Hey! This user doesn't have any stream alerts. Try that again please.");
                return;
            }
            //Save shouts.json.
            using (FileStream fs = File.Create("shouts.json"))
            {
                using (StreamWriter sw = new StreamWriter(fs))
                {
                    sw.WriteLine(JsonConvert.SerializeObject(shoutEntities, Formatting.Indented));//Output and save a file.
                    sw.Flush();
                }

            }
        }

        private async Task _client_GuildMemberUpdated(SocketGuildUser arg1, SocketGuildUser arg2)
        {
            if (arg1.Game?.StreamType == StreamType.NotStreaming && arg2.Game?.StreamType == StreamType.Twitch)//Event gets fired for every guild??
            {
                //add role.
                await arg2.AddRoleAsync(arg2.Guild.GetRole(arg2.Guild.Roles.FirstOrDefault(x => x.Name == "🔴 Live!").Id));
                await Client_Log(new LogMessage(LogSeverity.Info, "GoLive", arg2.Username + " went live! [Guild: "+arg2.Guild.Name+"]"));
                UserShout Shout = shoutEntities.FirstOrDefault(x => x.userID == arg2.Id);
                if (Shout != null)
                {
                    foreach (var item in Shout.ChannelIDs)
                    {
                        SocketTextChannel ch = _client.GetChannel(item) as SocketTextChannel;
                        string[] array = arg2.Game?.StreamUrl.Split('/');
                        if (ch.Guild == arg2.Guild)
                        {
                            if (Shout.SupressEveryone)
                            {
                                
                                await ch.SendMessageAsync(array.Last() + " just went live! " + arg2.Game?.StreamUrl);
                            }
                            else
                            {
                                await ch.SendMessageAsync("@everyone " + array.Last() + " just went live! " + arg2.Game?.StreamUrl);
                            }
                        }
                    }
                }

            }
            if (arg1.Game?.StreamType == StreamType.Twitch && arg2.Game?.StreamType == StreamType.NotStreaming)
            {
                //remove the role
                await arg2.RemoveRoleAsync(arg2.Guild.GetRole(arg2.Guild.Roles.FirstOrDefault(x => x.Name == "🔴 Live!").Id));
                await Client_Log(new LogMessage(LogSeverity.Info, "GoLive", arg2.Username + " Ended stream. [Guild: " + arg2.Guild.Name + "]"));
            }
        }

        public async Task Initialize()
        {
            Initialized = true;
            await Client_Log(new LogMessage(LogSeverity.Info, "GoLive", "Called Initialization. Checking all users. (This could take a minute)"));
            foreach (SocketGuild guild in _client.Guilds)
            {
                foreach (SocketGuildUser arg2 in guild.Users)
                {
                    if (arg2.Game?.StreamType == StreamType.Twitch)//Event gets fired twice??
                    {
                        await arg2.AddRoleAsync(arg2.Guild.GetRole(arg2.Guild.Roles.FirstOrDefault(x => x.Name == "🔴 Live!").Id));
                        await Client_Log(new LogMessage(LogSeverity.Info, "GoLive", arg2.Username + " is live!"));


                    }
                    if (arg2.Game?.StreamType == StreamType.NotStreaming)
                    {
                        await arg2.RemoveRoleAsync(arg2.Guild.GetRole(arg2.Guild.Roles.FirstOrDefault(x => x.Name == "🔴 Live!").Id));
                        await Client_Log(new LogMessage(LogSeverity.Info, "GoLive", arg2.Username + " is not live"));
                    }
                }
            }
            await Client_Log(new LogMessage(LogSeverity.Info, "GoLive", "Operation complete! Listening for further updates."));

        }

        public async Task ShoutTest(ICommandContext context)
        {

            await Client_Log(new LogMessage(LogSeverity.Info, "GoLive", "HEY!!! TEST!"));
            foreach (SocketGuild guild in _client.Guilds)
            {
                foreach (SocketGuildUser arg2 in guild.Users)
                {
                    if (arg2.Game?.StreamType == StreamType.Twitch)
                    {
                        await Client_Log(new LogMessage(LogSeverity.Info, "GoLive", "user streamtype twitch."+ arg2.Username));
                        await arg2.AddRoleAsync(arg2.Guild.GetRole(arg2.Guild.Roles.FirstOrDefault(x => x.Name == "🔴 Live!").Id));
                        await Client_Log(new LogMessage(LogSeverity.Info, "GoLive", arg2.Username + " is live!"));
                        //add role.
                        await Client_Log(new LogMessage(LogSeverity.Info, "GoLive", arg2.Username + " went live! [Guild: " + arg2.Guild.Name + "]"));
                        UserShout Shout = shoutEntities.FirstOrDefault(x => x.userID == arg2.Id);
                        if (Shout != null)
                        {
                            foreach (var item in Shout.ChannelIDs)
                            {
                                SocketTextChannel ch = _client.GetChannel(item) as SocketTextChannel;
                                string[] array = arg2.Game?.StreamUrl.Split('/');
                                if (ch.Guild == arg2.Guild)
                                {
                                    if (Shout.SupressEveryone)
                                    {

                                        await ch.SendMessageAsync(array.Last() + " just went live! " + arg2.Game?.StreamUrl);
                                    }
                                    else
                                    {
                                        await ch.SendMessageAsync("@everyone " + array.Last() + " just went live! " + arg2.Game?.StreamUrl);
                                    }
                                }
                            }
                        }


                    }
                    if (arg2.Game?.StreamType == StreamType.NotStreaming)
                    {
                        await arg2.RemoveRoleAsync(arg2.Guild.GetRole(arg2.Guild.Roles.FirstOrDefault(x => x.Name == "🔴 Live!").Id));
                        await Client_Log(new LogMessage(LogSeverity.Info, "GoLive", arg2.Username + " is not live"));
                    }
                }
            }
        }
    }

    public class UserShout
    {
        #region Properties
        public ulong userID { get; set; }
        public List<ulong> ChannelIDs { get; set; }
        public bool SupressEveryone { get; set; }
        #endregion

        /// <summary>
        /// Create a new UserShout data entry.
        /// </summary>
        /// <param name="User">the ID of the user</param>
        /// <param name="ChannelID">The channel where GoLive shoutouts will be posted</param>
        /// <param name="twitchUsername">The user's twitch username</param>
        /// <param name="supressEveryone">Prevent @everyone from being added to the shoutout.</param>
        public UserShout(ulong User, ulong ChannelID, bool supressEveryone)
        {
            userID = User;
            ChannelIDs = new List<ulong>();
            ChannelIDs.Add(ChannelID);
            SupressEveryone = supressEveryone;
        }

        /// <summary>
        /// Adds a channel to the list of channels to send the shoutout.
        /// </summary>
        /// <param name="id"></param>
        public void AddChannel(ulong id)
        {
            ChannelIDs.Add(id);
        }

        /// <summary>
        /// Removes a channel from the list of channels to send the shoutout.
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        public bool DeleteChannel(uint id)
        {
            return ChannelIDs.Remove(id);
        }
    }

    public class GoLiveModule: ModuleBase
    {
        public DiscordSocketClient _client { get; set; }
        public TwitchNotificationService _service { get; set; }
        public ConsoleLogWriter _writer { get; set; }
        public GoLiveModule(DiscordSocketClient discord, TwitchNotificationService service, ConsoleLogWriter writer)
        {
            _client = discord;
            this._writer = writer;
            _service = service;
        }

        [Command("AddAlert"), RequireUserPermission(GuildPermission.ManageGuild), Remarks("[CMDMgmt]")]
        public async Task AddAlert(IGuildUser user, bool SupressEveryone)
        {
            await _service.AddShoutEntity(Context, user as SocketGuildUser, SupressEveryone);
        }

        [Command("RemoveAlertChannel"), RequireUserPermission(GuildPermission.ManageGuild), Remarks("[CMDMgmt]")]
        public async Task delchalert(IGuildUser user)
        {
            await _service.RemoveEntityChannel(Context, user as SocketGuildUser);
        }
        [Command("RemoveAlert"), RequireUserPermission(GuildPermission.ManageGuild), Remarks("[CMDMgmt]")]
        public async Task delAlert(IGuildUser user)
        {
            await _service.RemoveShoutEntity(Context, user as SocketGuildUser);
        }
        [Command("AboutGoLive")]
        public async Task about()
        {
            EmbedBuilder builder = new EmbedBuilder();
            builder.WithAuthor(_client.CurrentUser);
            builder.WithTitle($"GoLive v{System.Reflection.Assembly.GetExecutingAssembly().GetName().Version.ToString(3)}");
            builder.Description = "A module for the twitch streamer. Offering visibility for other streamers in your guild. Below are supported commands.";
            builder.AddField("AddAlert", $"Users who have permission to manage the guild can add GoingLive alerts to a channel.\r\nUSAGE:`{Context.Message.Content[0].ToString()}AddAlert <discordUser> <TwitchUsername> <SupressEveryone>` where SupressEveryone will turn off @ everyone pings if true.");
            builder.AddField("InitGoLive", $"Add this command to the OnStart.core file., to enable the module when the bot starts. Otherwise, live alerts will not work.\r\nOnStart.core entry: ```DOS\r\nCMD InitGoLive\r\n```");
            builder.AddField("AboutGoLive", "Show this information again.");
            await Context.Channel.SendMessageAsync("",false,builder.Build());
        }
        [Command("InitGoLive",RunMode = RunMode.Async)]
        public async Task Init()
        {
            if(_service.Initialized)
            {
                await Context.Channel.SendMessageAsync("The GoLive module has already been started. No further action required.");
                return;
            }
            _writer.WriteEntry(new LogMessage(LogSeverity.Debug, "GoLive", "InitGoLive"));
            await _service.Initialize();
        }
        [Command("testGoLive", RunMode = RunMode.Async), RequireUserPermission(GuildPermission.ManageGuild), Remarks("[CMDMgmt]")]
        public async Task TEST_ALL_THINGS()
        {
           
            await _service.ShoutTest(Context);
        }
    }
}
