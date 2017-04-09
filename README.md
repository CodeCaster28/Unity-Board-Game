# Unity Simple Board Game

![alt tag](https://github.com/CodeCaster28/Unity-Board-Game/blob/master/preview.PNG?raw=true)

**Preamble**

Content provided here is free to use in any private, recreational manner for personal use only. Part of assets are free to use for non-commercial manner, so taking money for project with assets (sounds, textures, fonts e.t.c.) included in this repository is definitely forbidden. If you wish to use only code in your project, you are absolutelly permited to do this, even for commercial use, however keep in mind that this project is far from beign completed, playable product.

**Okay.. but what is it exactly?**

Simple board game implemented in Unity engine with code written in C#. It shows example of using A\* algorithm in path finding, you can set a path for your pawn by clicking any field on the scene, the shortest path will be then displayed as dots from your pawn to clicked field. Pressing SPACE will make your pawn move to destination location. Each player have his turn, then the cycle repeats. Whole turn cycle is using event system, keyboard and mouse Inputs are also driven by events. 

Project is using optimized singletons (with generic singleton class). Pawn movement on board are using linear interpolation function, so it's pretty smooth. Same with camera movement- you may be interested in using camera scripts separately, as it might be a good base for any project with free-look camera.

There is button for adding field when selecting any existing field on the scene. Pressing it will add new field automatically assigning new ID and connection between source field and target field. Tutorial included below.

**Instruction**

Move you camera around with W,S,A,D keys, rotate it holding RMB while moving the mouse, and zoom camera in/out with mouse wheel.
Each player can choose a path to move to, firstly select a path you wish to move and then press SPACE to make your pawn follow path. You can freerly reassign path if you wish to change it before before confirming it with Space.

TODO: Add field creation tutorial.
TODO: Add player tutorial.

**Major Features**

* Full featured camera with rotation, movement and zooming in/out
* Shortest path finding algorithm (A Star algorithm)
* Turn-based game flow using events
* Heroes M&M movement style (free travelling until movement points depletion)
* 4 players, up to 9 players supported on field (for no pawn overlapping)
* GUI Text fade animation using Animator
* Editor Script that adds new Field and automatically assign it's properties

**What is NOT included**

* Dice throw based on physics
* Algorithm showing all possible moves
* Special field events when something is happening
* Player statistics or collectibles of any kind
* GUI (game immediately starts, only "next turn" text is displayed)

**To Do**

* Tutorials!
* Safe removing field
* Fix empty link (null or mising) to any other node resulting in game crash
* Remember path unreached in previous turn
* Possibility to cancel movement
* Dice throw
* Handle multiple pawns in single field (now pawns are overlapping)
