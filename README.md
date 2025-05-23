# Auios.QuadTree

![Logo](https://raw.githubusercontent.com/Auios/Auios.QuadTree/main/Media/Logo.png)

[![Nuget](https://img.shields.io/nuget/v/Auios.QuadTree.svg?logo=nuget)](https://www.nuget.org/packages/Auios.QuadTree/1.2.0)
[![Nuget](https://img.shields.io/nuget/dt/Auios.QuadTree.svg)](https://www.nuget.org/packages/Auios.QuadTree/1.2.0)

A Generic QuadTree algorithm inspired by [Leonidovia's Ultimate QuadTree.](https://github.com/leonidovia/UltimateQuadTree)

Wikipedia: https://en.wikipedia.org/wiki/Quadtree

## Install

NuGet.org: <https://www.nuget.org/packages/Auios.QuadTree/>

```sh
Install-Package Auios.QuadTree
```

## Example

```cs
// Implement IQuadTreeObjectBounds<T> interface for the object type to be stored
public class MyCustomBounds : IQuadTreeObjectBounds<Vector2> {
    public float GetBottom(Vector2 obj) => obj.Y;
    public float GetTop(Vector2 obj) => obj.Y;
    public float GetLeft(Vector2 obj) => obj.X;
    public float GetRight(Vector2 obj) => obj.X;
}

// Create a QuadTree and fill it with objects
QuadTree<Vector2> quadTree = new QuadTree<Vector2>(800, 600, new MyCustomBounds());

// Generate some data to insert
Random random = new Random();
List<Vector2> myPositions = new List<Vector2>();
for(int i = 0; i < 1000; i++) {
    myPositions.Add(new Vector2((float)800 * random.NextDouble(), (float)600 * random.NextDouble()));
}

 // Insert data into the QuadTree
foreach(Vector2 position in myPositions) {
    quadTree.Insert(myObjects);
}

// Define search area (x, y, width, height)
QuadTreeRect searchArea = new QuadTreeRect(150, 100, 50, 25);

// Find objects in leaf quadrants which overlap the search area
Vector2[] positions = quadTree.FindObjects(searchArea);
```

## Demos

![Demo3](https://raw.githubusercontent.com/Auios/Auios.QuadTree/main/Media/Demo3.gif)

![Demo2](https://raw.githubusercontent.com/Auios/Auios.QuadTree/main/Media/Demo2.gif)

![Demo1](https://raw.githubusercontent.com/Auios/Auios.QuadTree/main/Media/Demo1.png)
