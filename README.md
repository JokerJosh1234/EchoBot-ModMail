# [EchoBot](https://github.com/JokerJosh1234/EchoBot) ModMail

## Setup
In the Handler.cs script, you need to set youre `CategoryId`, `GuildId`, and `LogChannel` Id

1. CategoryId
   - CategoryId is the category in which you want the mail to go to (created channel perms should match the category perms)

2. GuildId
   - GuildId is the guild in which the category is
  
3. LogChannel
   - LogChannel is the channel id for where you want the logs to go to (ticket creation and deletion), usually under the catergory where the mail goes to
  


## How it works
Once the bot gets a DM, it will create a ticket (channel) under the specified category (assuming its also under the same set guild) with the users message

Then a moderator can send a message in the ticket channel and it will DM that message to the user

moderators can use `=` at the start of their message to stop the message being set to the user (useful for moderator discussion)
moderators can also use `=close [reason]` to close the ticket which will delete the channel and notify the user with the reason

### Errors
if you receive an error with `Channel.IsDM()`, make sure youve updated your [echobot extentions](https://github.com/JokerJosh1234/EchoBot/blob/main/Extensions.cs) script to the newest version
