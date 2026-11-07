using Auios.QuadTree;
using Auios.QuadTree.Demos;
using Auios.QuadTree.Demos.Scenes;
using Raylib_cs;
using static Raylib_cs.Raylib;

// Usage:
//   dotnet run --project Auios.QuadTree.Demos
//   dotnet run --project Auios.QuadTree.Demos -- --capture <folder>   (saves a screenshot of every demo, then exits)

const int ScreenWidth = 1280;
const int ScreenHeight = 800;
const int CaptureFrames = 420;

string? captureFolder = args.Length >= 2 && args[0] == "--capture" ? Path.GetFullPath(args[1]) : null;
Pointer.AutoPilotOnly = captureFolder is not null;

SetConfigFlags(ConfigFlags.Msaa4xHint | ConfigFlags.VSyncHint);
SetTraceLogLevel(TraceLogLevel.Warning);
InitWindow(ScreenWidth, ScreenHeight, "Auios.QuadTree demos");
SetTargetFPS(0);
Ui.Load();
Sprites.Load();

QuadTreeRect world = new(0, 0, ScreenWidth, ScreenHeight);
Scene[] scenes = [
    new QueryScene(world),
    new CollisionScene(world),
    new FlockingScene(world),
    new SelectionScene(world),
];

int current = 0;
int frame = 0;
bool showPanel = true;

if (captureFolder is not null) {
    Directory.CreateDirectory(captureFolder);
}

while (!WindowShouldClose()) {
    for (int i = 0; i < scenes.Length; i++) {
        if (IsKeyPressed(KeyboardKey.One + i)) current = i;
    }
    if (IsKeyPressed(KeyboardKey.H)) showPanel = !showPanel;
    if (IsKeyPressed(KeyboardKey.G)) Settings.ShowGrid = !Settings.ShowGrid;

    Scene scene = scenes[current];
    scene.Update(MathF.Min(GetFrameTime(), 1 / 30f));

    BeginDrawing();
    ClearBackground(Theme.Background);
    scene.Draw();
    Ui.DrawTopBar(scenes, current);
    if (showPanel) Ui.DrawInfoPanel(scene);

    if (captureFolder is not null && ++frame == CaptureFrames) {
        Rlgl.DrawRenderBatchActive();
        Image image = LoadImageFromScreen();
        ExportImage(image, Path.Combine(captureFolder, $"{current + 1}-{scene.Slug}.png"));
        UnloadImage(image);
        frame = 0;
        if (++current == scenes.Length) {
            EndDrawing();
            break;
        }
    }

    EndDrawing();
}

Sprites.Unload();
Ui.Unload();
CloseWindow();
