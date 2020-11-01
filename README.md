# Auios.QuadTree

![Logo](https://github.com/Auios/Auios.QuadTree/blob/main/Media/Logo.png)

A Generic QuadTree algorithm inspired by [Leonidovia's Ultimate QuadTree.](https://github.com/leonidovia/UltimateQuadTree)

Wikipedia: https://en.wikipedia.org/wiki/Quadtree

## Install

NuGet.org: https://www.nuget.org/packages/Auios.QuadTree/

```
Install-Package Auios.QuadTree
```

## Example

```cs
// Implement IQuadTreeObjectBounds<T> interface for the object type to be stored
public class MyCustomBounds : IQuadTreeObjectBounds<Vector2>
{
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
for(int i = 0; i < 1000; i++)
{
    myPositions.Add(new Vector2((float)800 * random.NextDouble(), (float)600 * random.NextDouble()));
}

 // Insert data into the QuadTree
foreach(Vector2 position in myPositions)
{
    quadTree.Insert(myObjects);
}

// Define search area (x, y, width, height)
QuadTreeRect searchArea = new QuadTreeRect(150, 100, 50, 25);

// Find objects in leaf quadrants which overlap the search area
Vector2[] positions = quadTree.FindObjects(searchArea);
```

## Demos

![Demo3](https://github.com/Auios/Auios.QuadTree/blob/main/Media/Demo3.gif)

![Demo2](https://github.com/Auios/Auios.QuadTree/blob/main/Media/Demo2.gif)

![Demo1](https://github.com/Auios/Auios.QuadTree/blob/main/Media/Demo1.png)
