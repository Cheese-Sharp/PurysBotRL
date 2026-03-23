# RedUtilities

A set of utilities for making rocket league bots in C#

## Usage Instructions

### Prerequisites
Make sure you've installed [.NET SDK 8.0 x64](https://dotnet.microsoft.com/download).

Set up RLBotServer and RLBotGUI
1. Get the RLBot5 Launcher Installer from [here](https://github.com/RLBot/launcher/releases/tag/installer)
1. Run RLBot5 Launcher to get latest RLBotServer and RLBotGUI 
1. In the GUI, use Add -> Load folder in RLBotGUI on the current directory. This bot should appear in the list.

### Using Visual Studio
1. Install Visual Studio 2019 16.8 or newer.
1. Open Bot.sln in Visual Studio.
1. Edit the code as you see fit, and then compile 
1. In RLBotGUI, put the bot on a team and start the match.

### Using Rider
1. Install Rider. If you do not have Visual Studio installed alongside Rider, follow [this article](https://rider-support.jetbrains.com/hc/en-us/articles/207288089-Using-Rider-under-Windows-without-Visual-Studio-prerequisites) to set up Rider.
1. Open Bot.sln in Rider.
1. Edit the code as you see fit, and then compile
1. In RLBotGUI, put the bot on a team and start the match.

## Notes

- The original version of RedUtils was made for RLBot v4. This is a basic port of RedUtils to RLBot v5. I will continue to update this version until it is equivalent to the v4 version. Until then, feel free to make a pull request if anything isn't working!
- Bot name, description, etc, is configured by `bot.toml`
- Bot strategy is controlled by `Bot/Bot.cs`
- Bot appearance is controlled by `loadout.toml`
- To make your bot run as fast as possible, build it in release mode, and then change the "run_command" in `Bot.toml` to `.\\Bot\\bin\\Release\\net8.0\\Bot.exe`
- See the [wiki](https://github.com/RLBot/RLBotCSharpExample/wiki) for tips to improve your programming experience.
- If you'd like to keep up with bot strategies and bot tournaments, join our [Discord server](https://discord.gg/q9pbsWz). It's the heart of the RLBot community!

## Credit

-  [ddthj/GoslingUtils](https://github.com/ddthj/GoslingUtils) for inspiration on some of the structure and code (which I ported to c#)
-  [VirxEC/VirxERLU](https://github.com/VirxEC/VirxERLU) for the basis of my aerial code (which I ported to c#)
-  [Darxeal/BotimusPrime](https://github.com/Darxeal/BotimusPrime) for inspiration on some of the structure and driving code (which I ported to c#)
