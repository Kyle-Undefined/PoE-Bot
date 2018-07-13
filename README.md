<p align="center">
	<img src="https://i.imgur.com/BWsSbVi.png" />
	<h2 align="center">PoE Bot</h2>
	<p align="center">
		A bot for the Path Of Exile Xbox Discord server. Includes PoE Wiki Search, Community Pricing, Personal Shops, Tags, Fun & Game Commands, Twitch/Mixer Integration, RSS Feeds, Moderation, and more!
		<br/><br/>
		<a href="https://ci.appveyor.com/project/Kyle-Undefined/poe-bot"><img src="https://ci.appveyor.com/api/projects/status/n57hhid7qefr1vqa/branch/master"/></a>
		<a href="https://ravendb.net"><img src="https://img.shields.io/badge/Powered%20By-RavenDB-E50935.svg?longCache=true&style=flat-square"/></a>
		<a href="https://discord.gg/PGXQs4t"><img src="https://img.shields.io/badge/Join-PoE%20Xbox-7289DA.svg?longCache=true&style=flat-square&logo=discord"/></a>
	</p>
</p>

### Features

Here is just an overview of some of the features:

* Wiki Search
    * Looks up an item in the PoE Wiki database and returns the info about it
* Currency Pricing: 
    * Pricing data for Currency, done by the community until there's an API
* Personal Shops:
    * Have your own shop, and search for items from other players, and search by League
* PoE Leaderboards
    * New League and Race Leaderboards posted in the Discord every 30 minutes
* Path of Building
    * Display your Path of Building builds in the channels using the PasteBin export
* Trials of Ascendancy
    * Add/Remove Trials you need for Uber Lab, and notify/get notified when people come across them
* PoE Lab Notes integration
    * Automatically posts the updated Lab notes for Uber, Merciless, Cruel, and Normal lab.
* Hide/Show League Categories based on self assigned roles
* Useful & Fun Commands
    * User Reporting, Feedback, Reminders, Memes, etc
* Twitch/Mixer Integraion
* RSS Feeds
* Moderation
    * Mute, Kick, Ban, Warning System, Purge, Auto Mute, Profanity Filter, Rule Setup, Cases, etc
	
And many more!

### Credits

I wrote the bot a long time ago, even before the initial commit and it was just supposed to be a simple Wiki search bot. Over time more and more things were requested and it got to what it is today. During that time, I rewrote the bot as it needed it. The code was a mess, and was all over the place. I thought I had a much better system until I went to go work on it, or expand the features, it was a nightmare. 

Hanging around in the [Discord API Guild #dotnet_discord-net](https://discord.gg/jkrBmQR) channel I saw examples of the "proper" and better way of building a [Discord.Net](https://github.com/RogueException/Discord.Net) bot. I didn't understand any of it, and reading the [docs](https://docs.stillu.cc) only seemed to make it worse somehow.

I came across [Yucked](https://github.com/Yucked)'s bot [Valerie](https://github.com/Yucked/Valerie) and fell in love with how it was done. I was able to learn and study a well put together bot, and see how it all works. Admittedly, a huge portion of the new rework idea goes to him and I followed his design as it makes the most sense to me. He's a much smarter person than I. Go give him support, as he is much more deserving. Oh, and [buy him a coffee or three](https://www.buymeacoffee.com/Yucked)!

Also, thanks `Cidna#5074` for the logo!
