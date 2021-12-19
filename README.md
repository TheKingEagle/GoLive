# GoLive
A Twitch integration module for your discord bot. Designed for RMSoftware.ModularBOT

# NOTICE OF ARCHIVE
This module is not maintained since ModularBOT v2.0, and changes to the way discord handles presence.

### Source Code
**Users who want to build from source will need to reference the RMSoftwareModularBot project.**

# Installing
#### Please note: For usage examples we will be using the command prefix `!`. Remember to use the prefix you setup with the bot.
* Create a guild role called `🔴 Live!` (Yes, copy and paste the role name.)
* Make sure this role is below your bot's active role.
* Copy `GoLive.dll` &amp; `Services.GoLive.ini` into the `CMDModules` directory of your bot's installed directory. typically in your `AppData` folder.
* Open `OnStart.core` in your bot's main directory and add the following lines somewhere in the script:
```DOS
REM This command is required to run on bot startup. Please only run the command once.
CMD InitGoLive
```
* Restart the bot.
* You should see purple log entries in the console output titled GoLive.


# Creating Live Alerts
* Simply go into a text channel you want your bot to send the Alerts
* use the command `!addalert <DiscordUser @mention> <Twitch username> <True/False Supress @everyone>`
	* **Please note**: If you want your users to get an @everyone ping, make sure you specify `False` for the supress parameter.
	* **Please note**: Your bot must have the ability to mention @everyone in order to correctly use this command.

# FAQ &amp; Troubleshooting
### My bot isn't notifying a channel I went live?
* Does your bot have permission to send messages to your channel?
* Does your bot has the ability to post embeds and links in your text channel?
* This module will only work on Guild Channels. Not Groups or DM.
* Ensure you have used the `!addalert` command in that channel.
* Does your guild have the designated GoLive role? (`🔴 Live!`) Is this role below your bot's highest active role?
* Make sure you have enabled **Streamer Mode** in your settings. Otherwise, you won't get recognized as being live. By default, streamer mode is automatically enabled when you start OBS.
