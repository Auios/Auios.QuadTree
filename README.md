# Auios.QuadTree

![Logo](https://raw.githubusercontent.com/Auios/Auios.QuadTree/main/Media/Logo.png)

[![Nuget](https://img.shields.io/nuget/v/Auios.QuadTree.svg?logo=nuget)](https://www.nuget.org/packages/Auios.QuadTree/)
[![Nuget](https://img.shields.io/nuget/dt/Auios.QuadTree.svg)](https://www.nuget.org/packages/Auios.QuadTree/)

A generic QuadTree for .NET 10, inspired by [Leonidovia's Ultimate QuadTree](https://github.com/leonidovia/UltimateQuadTree).

Wikipedia: https://en.wikipedia.org/wiki/Quadtree

## Install

```sh
dotnet add package Auios.QuadTree
```

## Example

```cs
using System.Numerics;
using Auios.QuadTree;

// Implement IQuadTreeObjectBounds<T> for the type you store
public class MyCustomBounds : IQuadTreeObjectBounds<Vector2> {
    public float GetBottom(Vector2 obj) => obj.Y;
    public float GetTop(Vector2 obj) => obj.Y;
    public float GetLeft(Vector2 obj) => obj.X;
    public float GetRight(Vector2 obj) => obj.X;
}

QuadTree<Vector2> quadTree = new(800, 600, new MyCustomBounds());

Random random = new();
List<Vector2> myPositions = [];
for (int i = 0; i < 1000; i++) {
    myPositions.Add(new Vector2(800 * random.NextSingle(), 600 * random.NextSingle()));
}

quadTree.InsertRange(myPositions);

// Search region: x, y, width, height
QuadTreeRect searchArea = new(150, 100, 50, 25);

// Objects whose bounds intersect the search region
Vector2[] positions = quadTree.FindObjects(searchArea);
```

`FindObjects` returns objects whose bounds **intersect** the search rectangle (not every object in overlapping leaf nodes).

## Develop (Cursor / VS Code)

Requires the [.NET 10 SDK](https://dotnet.microsoft.com/download). For Test Explorer in the editor, install **C# Dev Kit** (or the C# extension).

```sh
dotnet test
dotnet pack Auios.QuadTree/Auios.QuadTree.csproj -c Release -o ./artifacts
```

Or run the VS Code / Cursor tasks: **build**, **test**, **pack** (pack runs tests first).

## Demos

![Demo3](https://raw.githubusercontent.com/Auios/Auios.QuadTree/main/Media/Demo3.gif)

![Demo2](https://raw.githubusercontent.com/Auios/Auios.QuadTree/main/Media/Demo2.gif)

![Demo1](https://raw.githubusercontent.com/Auios/Auios.QuadTree/main/Media/Demo1.png)
