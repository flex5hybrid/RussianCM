<<<<<<< HEAD
<div class="header" align="center">  
<img alt="Space Station 14" width="880" height="300" src="https://raw.githubusercontent.com/space-wizards/asset-dump/de329a7898bb716b9d5ba9a0cd07f38e61f1ed05/github-logo.svg">  
</div>
=======
<p align="center"> <img alt="Space Station 14" width="400" height="400" src="https://github.com/user-attachments/assets/320ad459-8997-4e5b-9f7e-fc7e7d7dcb73" /></p>
>>>>>>> master

RMC-14 is inspired by CM13, Space Station 13, that runs on [Robust Toolbox](https://github.com/space-wizards/RobustToolbox).

The design goal of this fork is to attempt to replicate the feel and experience of CM13, while using SS14 as a foundation.

This is the primary repo for RMC-14. To prevent people forking Robust Toolbox, a "content" pack is loaded by the client and server. This content pack contains everything needed to play the game on one specific server.

If you want to host or create content for RMC-14, this is the repo you need. It contains both RobustToolbox and the content pack for development of new content packs.

## Links

<<<<<<< HEAD
<div class="header" align="center">  

[Website](https://spacestation14.com/) | [Discord](https://discord.ss14.io/) | [Forum](https://forum.spacestation14.com/) | [Mastodon](https://mastodon.gamedev.place/@spacestation14) | [Lemmy](https://lemmy.spacestation14.com/) | [Patreon](https://www.patreon.com/spacestation14) | [Steam](https://store.steampowered.com/app/1255460/Space_Station_14/) | [Standalone Download](https://spacestation14.com/about/nightlies/)  

</div>

## Documentation/Wiki

Our [docs site](https://docs.spacestation14.com/) has documentation on SS14's content, engine, game design, and more.  
Additionally, see these resources for license and attribution information:  
- [Robust Generic Attribution](https://docs.spacestation14.com/en/specifications/robust-generic-attribution.html)  
- [Robust Station Image](https://docs.spacestation14.com/en/specifications/robust-station-image.html)

We also have lots of resources for new contributors to the project.

## Contributing

We are happy to accept contributions from anybody. Get in Discord if you want to help. We've got a [list of issues](https://github.com/space-wizards/space-station-14-content/issues) that need to be done and anybody can pick them up. Don't be afraid to ask for help either!  
Just make sure your changes and pull requests are in accordance with the [contribution guidelines](https://docs.spacestation14.com/en/general-development/codebase-info/pull-request-guidelines.html).

We are not currently accepting translations of the game on our main repository. If you would like to translate the game into another language, consider creating a fork or contributing to a fork.
=======
[RMC-14 Community Wiki](https://wiki.rouny-ss14.com/) | [Discord](https://discord.gg/rouny) | [SS14 Steam Launcher](https://store.steampowered.com/app/1255460/Space_Station_14/) | [Standalone Launcher Download](https://spacestation14.io/about/nightlies/)

## Contributing

We are happy to accept contributions from anybody. Get in Discord if you want to help. We've got a [list of issues](https://github.com/RMC-14/RMC-14/issues) that need to be done and anybody can pick them up. Don't be afraid to ask for help either!
>>>>>>> master

## Building

1. Clone this repo:
```shell
git clone https://github.com/space-wizards/space-station-14.git
```
2. Go to the project folder and run `RUN_THIS.py` to initialize the submodules and load the engine:
```shell
cd space-station-14
python RUN_THIS.py
```
3. Compile the solution:  

Build the server using `dotnet build`.

[More detailed instructions on building the project.](https://docs.spacestation14.com/en/general-development/setup.html)

## License

All code for the content repository is licensed under the [MIT license](https://github.com/space-wizards/space-station-14/blob/master/LICENSE.TXT).  

<<<<<<< HEAD
Most assets are licensed under [CC-BY-SA 3.0](https://creativecommons.org/licenses/by-sa/3.0/) unless stated otherwise. Assets have their license and copyright specified in the metadata file. For example, see the [metadata for a crowbar](https://github.com/space-wizards/space-station-14/blob/master/Resources/Textures/Objects/Tools/crowbar.rsi/meta.json).  
=======
Most assets are licensed under [CC-BY-SA-3.0](https://creativecommons.org/licenses/by-sa/3.0/) unless stated otherwise. Assets have their license and the copyright in the metadata file. [Example](https://github.com/space-wizards/space-station-14/blob/master/Resources/Textures/Objects/Tools/crowbar.rsi/meta.json).
>>>>>>> master

> [!NOTE]
> Some assets are licensed under the non-commercial [CC-BY-NC-SA 3.0](https://creativecommons.org/licenses/by-nc-sa/3.0/) or similar non-commercial licenses and will need to be removed if you wish to use this project commercially.
