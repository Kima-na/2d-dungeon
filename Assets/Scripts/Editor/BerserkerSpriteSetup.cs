#if UNITY_EDITOR
using System.IO;
using UnityEditor;
using UnityEngine;

public static class BerserkerSpriteSetup
{
    private const string Source = "Assets/Sprites/Berserker.png";
    private const string Output = "Assets/Resources/Berserker";
    private readonly struct Frame
    {
        public readonly string Name; public readonly RectInt Rect;
        public Frame(string name, int x, int top, int width, int height)
        { Name = name; Rect = new RectInt(x, top, width, height); }
    }
    private static readonly Frame[] Frames =
    {
        new("Idle_01",270,55,145,135), new("Idle_02",420,55,145,135), new("Idle_03",570,55,150,135),
        new("Walk_01",800,55,145,135), new("Walk_02",945,55,145,135), new("Walk_03",1075,55,200,135),
        new("Walk_04",1225,55,200,135), new("Walk_05",1420,55,155,135),
        new("Attack_01",270,235,150,140), new("Attack_02",420,235,150,140),
        new("Attack_03",570,235,130,140), new("Attack_04",720,235,170,140),
        new("Hit_01",930,400,150,145), new("Hit_02",1080,400,150,145),
        new("Hit_03",1230,400,150,145), new("Hit_04",1380,400,170,145),
        new("Death_01",270,570,150,135), new("Death_02",420,570,150,135),
        new("Death_03",570,570,150,135), new("Death_04",720,570,150,135),
        new("Death_05",870,570,150,135), new("Death_06",1020,570,150,135), new("Death_07",1170,570,170,135)
    };

    [MenuItem("Tools/2D Dungeon/Setup Berserker Sprite")]
    public static void Setup()
    {
        TextureImporter sourceImporter = AssetImporter.GetAtPath(Source) as TextureImporter;
        if (sourceImporter == null) { Debug.LogError("Berserker source missing: " + Source); return; }
        bool readable = sourceImporter.isReadable;
        sourceImporter.isReadable = true; sourceImporter.textureCompression = TextureImporterCompression.Uncompressed;
        sourceImporter.SaveAndReimport();
        EnsureFolder("Assets", "Resources"); EnsureFolder("Assets/Resources", "Berserker");
        Texture2D source = AssetDatabase.LoadAssetAtPath<Texture2D>(Source);
        foreach (Frame frame in Frames) WriteFrame(source, frame);
        AssetDatabase.Refresh();
        foreach (Frame frame in Frames) ConfigureSprite($"{Output}/{frame.Name}.png");
        CreatePrefab();
        sourceImporter = AssetImporter.GetAtPath(Source) as TextureImporter;
        sourceImporter.isReadable = readable; sourceImporter.SaveAndReimport();
        AssetDatabase.SaveAssets();
        Debug.Log($"BERSERKER_SPRITE_SETUP_PASS: {Frames.Length} frames and prefab created.");
    }

    private static void WriteFrame(Texture2D source, Frame frame)
    {
        const int canvas = 256; Color32[] output = new Color32[canvas * canvas];
        int minX=int.MaxValue,maxX=0,minY=int.MaxValue,maxY=0;
        var kept = new System.Collections.Generic.List<(int x,int y,Color32 c)>();
        for (int y=0;y<frame.Rect.height;y++) for(int x=0;x<frame.Rect.width;x++)
        {
            int sx=frame.Rect.x+x, sy=source.height-frame.Rect.y-frame.Rect.height+y;
            if(sx<0||sx>=source.width||sy<0||sy>=source.height) continue;
            Color32 c=source.GetPixel(sx,sy); if(c.r<=18&&c.g<=18&&c.b<=18) continue;
            kept.Add((x,y,c)); minX=Mathf.Min(minX,x); maxX=Mathf.Max(maxX,x); minY=Mathf.Min(minY,y); maxY=Mathf.Max(maxY,y);
        }
        if(frame.Name is "Walk_03" or "Walk_04")
            RemoveEdgeFragments(kept, frame.Rect.width, frame.Rect.height, false);
        minX=int.MaxValue;maxX=0;minY=int.MaxValue;maxY=0;
        foreach(var p in kept){minX=Mathf.Min(minX,p.x);maxX=Mathf.Max(maxX,p.x);minY=Mathf.Min(minY,p.y);maxY=Mathf.Max(maxY,p.y);}
        if(kept.Count==0) return;
        int width=maxX-minX+1,height=maxY-minY+1; float scale=Mathf.Min(1f,Mathf.Min(236f/width,236f/height));
        int ox=(canvas-Mathf.RoundToInt(width*scale))/2, oy=10;
        foreach(var p in kept){int x=ox+Mathf.RoundToInt((p.x-minX)*scale),y=oy+Mathf.RoundToInt((p.y-minY)*scale);if(x>=0&&x<canvas&&y>=0&&y<canvas)output[y*canvas+x]=p.c;}
        Texture2D texture=new(canvas,canvas,TextureFormat.RGBA32,false); texture.SetPixels32(output);texture.Apply();
        File.WriteAllBytes($"{Output}/{frame.Name}.png",texture.EncodeToPNG()); Object.DestroyImmediate(texture);
    }
    private static void RemoveEdgeFragments(System.Collections.Generic.List<(int x,int y,Color32 c)> pixels,int width,int height,bool preserveRight)
    {
        var byPosition=new System.Collections.Generic.Dictionary<int,int>();
        for(int i=0;i<pixels.Count;i++)byPosition[pixels[i].y*width+pixels[i].x]=i;
        var visited=new bool[pixels.Count];var components=new System.Collections.Generic.List<System.Collections.Generic.List<int>>();
        int[] offsets={-1,0,1};
        for(int start=0;start<pixels.Count;start++)
        {
            if(visited[start])continue;var component=new System.Collections.Generic.List<int>();var queue=new System.Collections.Generic.Queue<int>();
            visited[start]=true;queue.Enqueue(start);
            while(queue.Count>0)
            {
                int current=queue.Dequeue();component.Add(current);var p=pixels[current];
                foreach(int dy in offsets)foreach(int dx in offsets)
                {
                    if(dx==0&&dy==0)continue;
                    if(byPosition.TryGetValue((p.y+dy)*width+p.x+dx,out int next)&&!visited[next]){visited[next]=true;queue.Enqueue(next);}
                }
            }
            components.Add(component);
        }
        int largest=0;for(int i=1;i<components.Count;i++)if(components[i].Count>components[largest].Count)largest=i;
        int mainMinX=width,mainMaxX=0,mainMinY=height,mainMaxY=0;
        foreach(int index in components[largest]){var p=pixels[index];mainMinX=Mathf.Min(mainMinX,p.x);mainMaxX=Mathf.Max(mainMaxX,p.x);mainMinY=Mathf.Min(mainMinY,p.y);mainMaxY=Mathf.Max(mainMaxY,p.y);}
        var remove=new System.Collections.Generic.HashSet<int>();
        for(int i=0;i<components.Count;i++)
        {
            if(i==largest)continue;
            int minX=width,maxX=0,minY=height,maxY=0;
            foreach(int index in components[i]){var p=pixels[index];minX=Mathf.Min(minX,p.x);maxX=Mathf.Max(maxX,p.x);minY=Mathf.Min(minY,p.y);maxY=Mathf.Max(maxY,p.y);}
            int gapX=Mathf.Max(0,Mathf.Max(mainMinX-maxX,minX-mainMaxX));
            int gapY=Mathf.Max(0,Mathf.Max(mainMinY-maxY,minY-mainMaxY));
            if(gapX*gapX+gapY*gapY>144&&(!preserveRight||maxX<mainMinX))foreach(int index in components[i])remove.Add(index);
        }
        for(int i=pixels.Count-1;i>=0;i--)if(remove.Contains(i))pixels.RemoveAt(i);
    }
    private static void ConfigureSprite(string path)
    {
        TextureImporter importer=AssetImporter.GetAtPath(path) as TextureImporter;
        importer.textureType=TextureImporterType.Sprite;importer.spriteImportMode=SpriteImportMode.Single;importer.spritePixelsPerUnit=100f;
        importer.filterMode=FilterMode.Point;importer.textureCompression=TextureImporterCompression.Uncompressed;importer.alphaIsTransparency=true;
        importer.spritePivot=new Vector2(0.5f,0.05f);importer.SaveAndReimport();
    }
    private static void CreatePrefab()
    {
        GameObject root=new("Berserker",typeof(SpriteRenderer),typeof(Rigidbody2D),typeof(BoxCollider2D),typeof(Damageable),typeof(EnemyAI),typeof(BerserkerVisualAnimator));
        try
        {
            root.GetComponent<SpriteRenderer>().sprite=Load("Idle_01"); SerializedObject animator=new(root.GetComponent<BerserkerVisualAnimator>());
            Set(animator,"idle","Idle",3);Set(animator,"walk","Walk",5);Set(animator,"attack","Attack",4);Set(animator,"hit","Hit",4);Set(animator,"death","Death",7);
            animator.ApplyModifiedPropertiesWithoutUndo();PrefabUtility.SaveAsPrefabAsset(root,$"{Output}/Berserker.prefab");
        }
        finally { Object.DestroyImmediate(root); }
    }
    private static void Set(SerializedObject target,string property,string prefix,int count)
    {SerializedProperty array=target.FindProperty(property);array.arraySize=count;for(int i=0;i<count;i++)array.GetArrayElementAtIndex(i).objectReferenceValue=Load($"{prefix}_{i+1:00}");}
    private static Sprite Load(string name)=>AssetDatabase.LoadAssetAtPath<Sprite>($"{Output}/{name}.png");
    private static void EnsureFolder(string parent,string child){if(!AssetDatabase.IsValidFolder(parent+"/"+child))AssetDatabase.CreateFolder(parent,child);}
}
#endif
