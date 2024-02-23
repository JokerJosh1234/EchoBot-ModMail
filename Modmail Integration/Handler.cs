using Discord;
using Discord.Rest;
using Discord.WebSocket;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace Modmail_Integration
{
    public class Handler :  Event
    {
        public const ulong CategoryId = 0;
        public const ulong GuildId = 0;
        public const ulong LogChannel = 0;

        private SocketMessage message;
        private SocketGuild guild;
        private SocketGuildUser currentUser;
        private SocketGuildUser ticketOwner;


        public override async Task MessageReceived(SocketMessage message)
        {
            if (message.Author.IsBot) return;
            this.message = message;
            guild = Hub.client.Guilds.FirstOrDefault(x => x.Id == GuildId);
            currentUser = guild.GetUser(message.Author.Id);


            var socketMessage = message.Channel as SocketTextChannel;

            if (message.Channel.IsDM())
                await CreateTicket();
            else if (socketMessage != null && socketMessage.CategoryId == CategoryId && !string.IsNullOrEmpty(socketMessage.Topic))
            {
                if (!string.IsNullOrEmpty(socketMessage?.Topic))
                    ticketOwner = guild.GetUser(ulong.Parse(socketMessage.Topic));
                await HandleTicketMessage();
            }

        }

        public async Task HandleTicketMessage()
        {
            // returns if the user isnt in the specified server
            if (ticketOwner == null)
            {
                await (message.Channel as SocketGuildChannel).DeleteAsync();
                await Log("Ticket Closed", "User no longer in server");
                return;
            }

            if (!message.Content.StartsWith("=") && !message.Content.StartsWith("=close"))
            {
                EmbedBuilder receivedMessage = new EmbedBuilder()
                        .WithTitle("Message Received")
                        .WithColor(Color.Blue)
                        .WithDescription(message.Content)
                        .WithFooter(new EmbedFooterBuilder { Text = $"{ticketOwner.Username} | {ticketOwner.Id}", IconUrl = ticketOwner.GetAvatarUrl() })
                        .WithCurrentTimestamp();

                EmbedBuilder sentMessage = new EmbedBuilder()
                        .WithTitle("Message Sent")
                        .WithAuthor(new EmbedAuthorBuilder { Name = currentUser.Username, IconUrl = currentUser.GetAvatarUrl() })
                        .WithColor(Color.Green)
                        .WithDescription(message.Content)
                        .WithFooter(new EmbedFooterBuilder { Text = $"{ticketOwner.Username} | {ticketOwner.Id}", IconUrl = ticketOwner.GetAvatarUrl() })
                        .WithCurrentTimestamp();

                await message.DeleteAsync();

                await message.Channel.SendMessageAsync(embed: sentMessage.Build());
                await currentUser.SendMessageAsync(embed: receivedMessage.Build());
            }else if (message.Content.StartsWith("=close"))
            {
                string closeReason = !string.IsNullOrWhiteSpace(string.Join(" ", message.Content.Split(' ').Skip(1))) ? string.Join(" ", message.Content.Split(' ').Skip(1)) : "No reason given.";


                await Log("Ticket Closed", closeReason);

                await (message.Channel as SocketTextChannel).DeleteAsync();

                EmbedBuilder ticketClosed = new EmbedBuilder()
                        .WithTitle("Ticket Closed")
                        .WithAuthor(new EmbedAuthorBuilder { Name = currentUser.Username, IconUrl = currentUser.GetAvatarUrl() })
                        .WithColor(Color.Red)
                        .WithDescription(closeReason)
                        .WithFooter(new EmbedFooterBuilder { Text = $"{ticketOwner.Username} | {ticketOwner.Id}", IconUrl = ticketOwner.GetAvatarUrl() })
                        .WithCurrentTimestamp();

                await ticketOwner.SendMessageAsync(embed: ticketClosed.Build());
            }
        }

        public async Task CreateTicket()
        {
            // returns if the user isnt in the specified server
            if (currentUser == null) return;

            var category = (SocketCategoryChannel)Hub.client.GetChannel(CategoryId);

            if (category != null)
            {
                string sanitizedName = Regex.Replace(message.Author.Username, @"[^\w\d]", "");
                var textChannel = category.Guild.Channels.OfType<SocketTextChannel>()
                    .FirstOrDefault(x => x.Category == category && x.Topic == message.Author.Id.ToString());

                EmbedBuilder receivedMessage = new EmbedBuilder()
                        .WithTitle("Message Received")
                        .WithColor(Color.Blue)
                        .WithDescription(message.Content)
                        .WithFooter(new EmbedFooterBuilder { Text = $"{currentUser.Username} | {currentUser.Id}", IconUrl = currentUser.GetAvatarUrl() })
                        .WithCurrentTimestamp();

                if (textChannel == null)
                {
                    var restTextChannel = await category.Guild.CreateTextChannelAsync(sanitizedName, properties =>
                    {
                        properties.CategoryId = category.Id;
                        properties.Topic = message.Author.Id.ToString();
                    });


                    EmbedBuilder newTicket = new EmbedBuilder()
                        .WithTitle("New Ticket")
                        .WithColor(Color.Blue)
                        .WithDescription("Messages starting with the server prefix `=` are ignored, and can be used for discussion. Use the command `=close [reason]` to close this ticket.")
                        .AddField("User", $"{currentUser.Mention}\n({currentUser.Id})", true)
                        .AddField("Roles", string.Join(" ", string.Join(" ", currentUser.Roles.Select(role => role.Mention))), true)
                        .WithFooter(new EmbedFooterBuilder { Text = $"{currentUser.Username} | {currentUser.Id}", IconUrl = currentUser.GetAvatarUrl() })
                        .WithCurrentTimestamp();

                    await Log("New Ticket");

                    await restTextChannel.SendMessageAsync(embed: newTicket.Build());
                    await restTextChannel.SendMessageAsync(embed: receivedMessage.Build());

                    EmbedBuilder sentMessage = new EmbedBuilder()
                      .WithTitle("Message Sent")
                      .WithColor(Color.Blue)
                      .WithDescription(message.Content)
                      .WithCurrentTimestamp();

                    await message.Channel.SendMessageAsync(embed: sentMessage.Build());
                }
                else
                {
                    EmbedBuilder sentMessage = new EmbedBuilder()
                      .WithTitle("Message Sent")
                      .WithColor(Color.Blue)
                      .WithDescription(message.Content)
                      .WithCurrentTimestamp();

                    await textChannel.SendMessageAsync(embed: receivedMessage.Build());
                    await message.Channel.SendMessageAsync(embed: sentMessage.Build());
                }
            }
            else
                Console.WriteLine("No category found");
        }


        public async Task Log(string title, string desc = null)
        {
            EmbedBuilder embed = new EmbedBuilder()
                .WithTitle(title)
                .WithCurrentTimestamp();

            if (desc != null)
                embed.WithDescription(desc);

            if (currentUser != null)
                embed.WithFooter(new EmbedFooterBuilder { Text = $"{currentUser.Username} | {currentUser.Id}", IconUrl = currentUser.GetAvatarUrl() });

            if (ticketOwner != null)
                embed.WithFooter(new EmbedFooterBuilder { Text = $"{ticketOwner.Username} | {ticketOwner.Id}", IconUrl = ticketOwner.GetAvatarUrl() });

            await (guild.GetChannel(LogChannel) as SocketTextChannel).SendMessageAsync(embed: embed.Build());
        }
    }
}
