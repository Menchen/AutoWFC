# AutoWFC - Unity Plugin for Tilemap Generation using Wave Function Collapse

AutoWFC is a powerful Unity plugin that utilizes the Wave Function Collapse algorithm to generate intricate and diverse tilemaps effortlessly. With AutoWFC, you can create tile-based environments with ease, thanks to its range of features. These features include the ability to learn from existing tilesets and specific regions of a tilemap. Additionally, AutoWFC includes a custom editor window called the "Pattern Explorer," which allows you to visualize and analyze the relationships within the current tileset model.

![Example of AutoWFC in action](https://i.imgur.com/NUOn8hY.gif)


![Example of WFC filling a region](https://i.imgur.com/dV64jei.gif)



## Features

- **Wave Function Collapse**: AutoWFC implements the Wave Function Collapse algorithm, a powerful technique for generating tilemaps based on input constraints. It ensures that the resulting tilemaps obey the provided rules and exhibit the desired patterns.

- **Tileset Learning**: AutoWFC allows you to feed it with existing tilesets, enabling the plugin to learn from their patterns and relationships. This learning capability helps the algorithm generate tilemaps that closely resemble the input tilesets.

- **Region-based Learning**: In addition to learning from entire tilesets, AutoWFC can focus on specific regions of a tilemap for learning purposes. This feature is particularly useful when you want to maintain the characteristics of a specific area in your generated tilemap.

- **Pattern Explorer**: AutoWFC provides a custom editor window called the Pattern Explorer. This tool allows you to explore and visualize the relationships and patterns present in the current tileset model. By examining these patterns, you can fine-tune the behavior of the generation process to achieve the desired results.

## Installation

### Install via git URL

To install AutoWFC via the git URL, follow these steps:

1. Open the Unity Package Manager (Windows/Package Manager).
2. Add `https://github.com/Menchen/AutoWFC.git#upm` to the Package Manager.

![Adding package from git URL](https://i.imgur.com/rEFWeFX.png)

![Unity Package Manager with AutoWFC](https://i.imgur.com/jJbFn4B.png)

### Install manually

To manually install AutoWFC, follow these steps:

1. Clone the AutoWFC repository.
2. Locate the folder named `Packages/me.menchen.autowfc` in the cloned repository.
3. Copy the entire `me.menchen.autowfc` folder.
4. Navigate to the project where you want to install the package.
5. Find the `Package` folder within the project.
6. Paste the copied `me.menchen.autowfc` folder into the `Package` folder.

## Getting Started

To get started with AutoWFC, follow these steps:

1. **Prepare your Tilesets**: Move the sprite sheet into a folder named `Resources`. Slice the tileset as you would normally do using the *Sprite Editor*, and make sure that `Read/Write` is enabled under `Advanced` before saving.
2. **Attach Wfc Helper Component**: Add the `Wfc Helper` component to the same game object as the `Tilemap`.
3. **Set Tile Output Folder**: Prepare a folder where the generated tiles will be stored for use by WFC.
4. **Create Model**: Under the `Wfc Helper`, create a new model from the tile set by specifying the tile set in the `Tile Set` field. Alternatively, you can enable grid selection by clicking on `Click Here to toggle grid selection`, select a region by dragging in the scene view, and use `Create new model from selection`.
5. **Generate Tiles**: After training a model, select a region in the tile map by enabling `Click Here totoggle grid selection` and then dragging the selection in the scene view. Once you have an active selection, press `Generate tiles from model`.

During step 5, you can generate variants by pressing `Generate tiles from model` again or undo the generation with `Ctrl/Cmd-Z`. If you want to train more information into an existing model, you can press `Train from selected region`. To delete a relationship, select a region of size 2 and press `Unlearn from selection`. Alternatively, you can manually modify the patterns in the Pattern Explorer by pressing `Open Pattern Explorer`.

## Pattern Explorer

To open the Pattern Explorer, follow these steps:

1. Click on `Open Pattern Explorer` in the `Wfc Helper` or access it through the menu bar by selecting "Windows/WFC Pattern Explorer".

In the Pattern Explorer, you can perform the following actions:

- **Selecting a Tile**: By dragging a tile to the center position, you can view its neighbors' hash, entropy, frequency, and more. Hovering the mouse over a tile displays the direction it belongs to and its hash. By dragging a tile to one of the four direction buttons, you can toggle its relationship with the center tile in that direction.

![Pattern Explorer - Selecting a Tile](https://i.imgur.com/XMGo6ZT.png)

![Pattern Explorer - Tile Relationship](https://i.imgur.com/L6bgvdQ.png)

## Contributions and Issues

If you encounter any issues, bugs, or have suggestions for improvements, please feel free to create an issue. Contributions, pull requests, and feedback from the community are highly appreciated and welcomed.

## License

AutoWFC is released under the [MIT License](https://opensource.org/licenses/MIT), granting you the freedom to use, modify, and distribute the plugin according to the terms of the license.

## Acknowledgments

AutoWFC was inspired by and built upon the foundational concepts of the [Wave Function Collapse algorithm](https://github.com/mxgmn/WaveFunctionCollapse). We would like to express our gratitude to the original creators and contributors of the algorithm for their groundbreaking work.