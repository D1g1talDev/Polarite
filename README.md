# Polarite

A simple multiplayer mod for **ULTRAKILL** inspired by **Jaket**.

Works with **Steam**

[![Watch the trailer](https://img.youtube.com/vi/RvO-dnHlCfE/maxresdefault.jpg)](https://www.youtube.com/watch?v=RvO-dnHlCfE)


## What Syncs
- Player movement, animations, and health  
- Weapons equipped
- Respawning, and dying
- Enemy spawns, Enemy positions and rotations, damage to enemies
- Breakables, hook points, checkpoints
- Arenas

## Manual Installation
1. Make sure you have **BepInEx** for ULTRAKILL
2. Extract the **downloaded ZIP**
3. Drop **the extracted folder** into your `BepInEx/plugins` folder  
4. Launch ULTRAKILL


## COMPILING (NORMAL USERS DONT NEED TO DO THIS!)

1. in jaketlite folder create a folder named "libs"
2. copy every dll inside of ULTRAKILL/ULTRAKILL_Data/Managed
2.5. place inside of the libs folder
3. copy 0Harmony.dll and Bepinex.dll and every mono cecil file
4. do the same step as 2.5
5. build like a normal project

notes: if you wanna add a cs file, edit csproj and scroll down and say to add it, i dont know why the owner isnt using a moderner dotnet sdk for this project but im just a contributor