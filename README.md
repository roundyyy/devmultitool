# DevMultiTool for Unity

<img src="https://beeimg.com/images/h53473132103.png" alt="Unity Toolbox Logo" width="400"/>



DevMultiTool for Unity is a set of tools that can help you to make boring tasks faster by performing batch operations on game objects. With this tool, you can focus on development and spend less time on doing repetitive stuff. About 30 different editor tools that I find useful in my work (some more, some less).
Tools was developed to help me work with WarDust VR game and also help modders, so some parts of the code are specific for this game. But it can be also used as general editor helper.

## Features

Tested on Unity 2019.4.40 and 2020.3.47, but it should work on others as well (I think)

### Batch Operations

DevMultiTool provides several batch operations that can be performed on game objects. These include:

- Moving, cloning, replacing, enabling, disabling, setting, adjusting etc.
- Sorting game objects by material with submeshes separation support
- Setting layer and tag for multiple game objects
- Adding/removing/cloning colliders in bulk with layer set.
- Cleaning empty game objects or inactive components
- Search objects by layer or size
- Converting terrain trees to scene game objects
- Sorting prefabs by type and placing in to single parent
- and more...

### Optimization Tools

DevMultiTool also includes some tools for optimizing your Unity projects. These include:

- Material Baker. It will bake cheap version of your materials in to 1 texture (custom shaders supported), so it can be used for lower LOD levels.
- LOD tweaker allows you to set LOD groups in batch to switch LOD levels in actual distance (in units), rather than screen relative size (approx)

### Scene Management

DevMultiTool provides features to make working with large scenes easier.


## Getting Started

Download here : [DevMultiTool](https://github.com/roundyyy/devmultitool/releases)

To use DevMultiTool, simply import the package into your Unity project. Once imported, you can access the tools by opening Tools/DevMultiTool.

## Contributing

If you'd like to contribute to DevMultiTool, feel free to fork this repository and submit a pull request. We welcome contributions of all kinds, including bug reports, feature requests, and code changes.

### Detailed and explained list of features

TO BE ADDED !

### Tutorials

TO BE ADDED !

[YOUTUBE : Material snapshot bake](https://youtu.be/Q-TPyfi5Tn4)

[YOUTUBE : Exporting trees and creating colliders on bark](https://youtu.be/pzjH0LEvdxE)

### About

This is my first that big editor tool, so code is bit messy, same as GUI ;)
I take no responsibilty for breaking your project, scenes, car or leg. Some features have UNDO support, but I suggest to save your scenes and keep always project backups up to date.


