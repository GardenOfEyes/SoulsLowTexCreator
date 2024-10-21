# SoulsLowTexCreator
One of the time consuming tasks when making mods in Souls games is the need to create _l versions of game texture files (mainly characters, assets, parts, etc...), in order to have them load in an optimized manner in the game, avoiding white flickering, textures not loading from afar, equipment not appearing for other players, and many other issues.

This tool currently supports the following file formats:
- *.partsbnd.dcx *(Parts files of weapons & armors)*
- *.texbnd.dcx *(Character texture files, you at least need to have the _h version, and it will use that to create the _l version)*
- *.tpf.dcx *(For Elden Ring, used mainly for assets' AET texture files)*

 What the tool does, is it reads those files, automatically halves the size of the textures (since the textures are supposed to be lower quality), and repacks them into the new _l files.
 

# Instructions
1) Download the latest release: https://github.com/GardenOfEyes/SoulsLowTexCreator/releases/tag/release
2) Export the .zip file
3) Drag the folder that has your files into the **SoulsLowTexCreator.exe**
4) The tool will create the _l version in the respective folders!
5) Enjoy!

# Credits
- Everyone who contributed to making SoulsFormats: https://github.com/soulsmods/SoulsFormatsNEXT
