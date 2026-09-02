#if UNITY_EDITOR
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

public static class AncientGolemBossSetup
{
    private const string Source="Assets/Sprites/hard boss anicent golem.png", Output="Assets/Resources/AncientGolem", Prefab=Output+"/AncientGolemBoss.prefab";
    private readonly struct Frame { public readonly string name; public readonly RectInt rect; public Frame(string n,int x,int y,int w,int h){name=n;rect=new RectInt(x,y,w,h);} }
    private static readonly Frame[] Frames={
        new("P1Idle_01",365,60,112,135),new("P1Idle_02",478,60,108,135),new("P1Idle_03",588,60,112,135),new("P1Idle_04",704,60,116,135),
        new("P1Slam_01",840,60,118,145),new("P1Slam_02",965,60,110,145),new("P1Slam_03",1078,60,118,145),new("P1Slam_04",1200,60,122,145),new("P1Slam_05",1340,45,178,165),
        new("P1Throw_01",365,255,116,130),new("P1Throw_02",480,255,102,130),new("P1Hit_01",838,250,116,140),new("P1Hit_02",968,250,122,140),
        new("P1Projectile_01",588,267,64,50),new("P1Projectile_02",656,274,48,45),new("P1Projectile_03",718,281,44,42),new("P1Projectile_04",768,288,55,38),
        new("P1Death_01",1108,255,136,140),new("P1Death_02",1248,255,145,140),new("P1Death_03",1397,255,139,140),
        new("P2Idle_01",382,470,118,150),new("P2Idle_02",510,470,120,150),new("P2Idle_03",641,470,122,150),new("P2Idle_04",775,470,130,150),
        new("P2Slam_01",928,468,127,155),new("P2Slam_02",1058,468,124,155),new("P2Slam_03",1185,468,149,155),new("P2Slam_04",1335,455,190,170),
        new("P2Crack_01",381,672,120,132),new("P2Crack_02",513,670,176,134),new("P2Crack_03",697,668,237,136),
        new("P2Volley_01",939,672,112,132),new("P2Volley_02",1070,672,111,132),
        new("P2Projectile_01",1206,675,105,42),new("P2Projectile_02",1355,670,115,48),
        new("P2Projectile_03",1206,720,122,36),new("P2Projectile_04",1332,718,78,44),new("P2Projectile_05",1442,720,82,48),
        new("P2Projectile_06",1206,766,122,45),new("P2Projectile_07",1352,766,108,45),
        new("P2Hit_01",377,846,114,166),new("P2Hit_02",499,846,112,166),new("P2Hit_03",616,846,117,166),new("P2Hit_04",743,846,109,166),
        new("P2Death_01",886,858,129,154),new("P2Death_02",1020,858,146,154),new("P2Death_03",1170,878,168,134),new("P2Death_04",1340,878,190,134)};

    [MenuItem("Tools/2D Dungeon/Setup Ancient Golem Boss")]
    public static void Setup(){ var importer=AssetImporter.GetAtPath(Source) as TextureImporter; if(importer==null){Debug.LogError("Ancient Golem source missing");return;} bool readable=importer.isReadable; importer.isReadable=true; importer.textureCompression=TextureImporterCompression.Uncompressed; importer.SaveAndReimport(); EnsureFolders(); Texture2D source=AssetDatabase.LoadAssetAtPath<Texture2D>(Source); foreach(var frame in Frames) Write(source,frame); AssetDatabase.Refresh(); foreach(var frame in Frames) Configure(Output+"/"+frame.name+".png"); CreatePrefab(); importer=(TextureImporter)AssetImporter.GetAtPath(Source); importer.isReadable=readable; importer.SaveAndReimport(); AssetDatabase.SaveAssets(); Debug.Log("ANCIENT_GOLEM_SETUP_PASS: "+Frames.Length+" frames and HARD boss prefab created."); }

    [MenuItem("Tools/2D Dungeon/Re-extract Ancient Golem Phase 2")]
    public static void ReextractPhaseTwo()
    {
        var importer=AssetImporter.GetAtPath(Source) as TextureImporter;
        if(importer==null){Debug.LogError("Ancient Golem source missing");return;}
        bool readable=importer.isReadable;
        importer.isReadable=true;
        importer.textureCompression=TextureImporterCompression.Uncompressed;
        importer.SaveAndReimport();
        EnsureFolders();
        Texture2D source=AssetDatabase.LoadAssetAtPath<Texture2D>(Source);
        int extracted=0;
        foreach(var frame in Frames)
        {
            if(!frame.name.StartsWith("P2"))continue;
            Write(source,frame);
            extracted++;
        }
        AssetDatabase.Refresh();
        foreach(var frame in Frames)if(frame.name.StartsWith("P2"))Configure(Output+"/"+frame.name+".png");
        importer=(TextureImporter)AssetImporter.GetAtPath(Source);
        importer.isReadable=readable;
        importer.SaveAndReimport();
        AssetDatabase.SaveAssets();
        Debug.Log("ANCIENT_GOLEM_PHASE2_REEXTRACT_PASS: "+extracted+" frames recreated.");
    }

    [MenuItem("Tools/2D Dungeon/Re-extract Ancient Golem Projectiles")]
    public static void ReextractProjectiles()
    {
        var importer=AssetImporter.GetAtPath(Source) as TextureImporter;
        if(importer==null){Debug.LogError("Ancient Golem source missing");return;}
        bool readable=importer.isReadable;
        importer.isReadable=true;
        importer.textureCompression=TextureImporterCompression.Uncompressed;
        importer.SaveAndReimport();
        EnsureFolders();
        Texture2D source=AssetDatabase.LoadAssetAtPath<Texture2D>(Source);
        int extracted=0;
        foreach(var frame in Frames)
        {
            if(!frame.name.Contains("Projectile"))continue;
            Write(source,frame);extracted++;
        }
        AssetDatabase.Refresh();
        foreach(var frame in Frames)if(frame.name.Contains("Projectile"))Configure(Output+"/"+frame.name+".png");
        importer=(TextureImporter)AssetImporter.GetAtPath(Source);
        importer.isReadable=readable;
        importer.SaveAndReimport();
        AssetDatabase.SaveAssets();
        Debug.Log("ANCIENT_GOLEM_PROJECTILE_REEXTRACT_PASS: "+extracted+" frames recreated.");
    }
private static void Write(Texture2D source,Frame frame)
    {
        const int size=256;
        int width=frame.rect.width,height=frame.rect.height;
        Color32[] crop=new Color32[width*height];
        bool[] background=new bool[crop.Length];
        Queue<Vector2Int> pending=new();

        for(int y=0;y<height;y++)for(int x=0;x<width;x++)
        {
            int sx=frame.rect.x+x,sy=source.height-frame.rect.y-frame.rect.height+y;
            crop[y*width+x]=(sx>=0&&sx<source.width&&sy>=0&&sy<source.height)?source.GetPixel(sx,sy):new Color32(255,255,255,255);
        }

        bool IsWhite(Color32 c)=>c.a<8||(c.r>188&&c.g>188&&c.b>188&&Mathf.Max(c.r,Mathf.Max(c.g,c.b))-Mathf.Min(c.r,Mathf.Min(c.g,c.b))<32);
        void Seed(int x,int y){int index=y*width+x;if(!background[index]&&IsWhite(crop[index])){background[index]=true;pending.Enqueue(new Vector2Int(x,y));}}
        for(int x=0;x<width;x++){Seed(x,0);Seed(x,height-1);}
        for(int y=0;y<height;y++){Seed(0,y);Seed(width-1,y);}
        int[] dx={-1,1,0,0,-1,-1,1,1},dy={0,0,-1,1,-1,1,-1,1};
        while(pending.Count>0){Vector2Int p=pending.Dequeue();for(int n=0;n<8;n++){int nx=p.x+dx[n],ny=p.y+dy[n];if(nx>=0&&nx<width&&ny>=0&&ny<height)Seed(nx,ny);}}

        Queue<Vector2Int> internalWhite=new();
        for(int y=0;y<height;y++)for(int x=0;x<width;x++)
        {
            int index=y*width+x;
            Color32 c=crop[index];
            int brightest=Mathf.Max(c.r,Mathf.Max(c.g,c.b));
            int darkest=Mathf.Min(c.r,Mathf.Min(c.g,c.b));
            if(!background[index]&&c.r>235&&c.g>235&&c.b>235&&brightest-darkest<16)
            {
                background[index]=true;
                internalWhite.Enqueue(new Vector2Int(x,y));
            }
        }
        while(internalWhite.Count>0)
        {
            Vector2Int p=internalWhite.Dequeue();
            for(int n=0;n<8;n++)
            {
                int nx=p.x+dx[n],ny=p.y+dy[n];
                if(nx<0||nx>=width||ny<0||ny>=height)continue;
                int index=ny*width+nx;
                if(background[index])continue;
                Color32 c=crop[index];
                int brightest=Mathf.Max(c.r,Mathf.Max(c.g,c.b));
                int darkest=Mathf.Min(c.r,Mathf.Min(c.g,c.b));
                if(c.r>130&&c.g>130&&c.b>130&&brightest-darkest<42)
                {
                    background[index]=true;
                    internalWhite.Enqueue(new Vector2Int(nx,ny));
                }
            }
        }

        for(int pass=0;pass<3;pass++)
        {
            List<int> fringe=new();
            for(int y=0;y<height;y++)for(int x=0;x<width;x++)
            {
                int index=y*width+x;
                if(background[index])continue;
                Color32 c=crop[index];
                int brightest=Mathf.Max(c.r,Mathf.Max(c.g,c.b));
                int darkest=Mathf.Min(c.r,Mathf.Min(c.g,c.b));
                if(c.r<=135||c.g<=135||c.b<=135||brightest-darkest>=38)continue;
                bool touchesBackground=false;
                for(int n=0;n<8;n++)
                {
                    int nx=x+dx[n],ny=y+dy[n];
                    if(nx<0||nx>=width||ny<0||ny>=height||background[ny*width+nx]){touchesBackground=true;break;}
                }
                if(touchesBackground)fringe.Add(index);
            }
            foreach(int index in fringe)background[index]=true;
            if(fringe.Count==0)break;
        }

        bool[] visited=new bool[crop.Length];
        for(int start=0;start<crop.Length;start++)
        {
            if(background[start]||visited[start])continue;
            List<int> component=new();
            Queue<int> componentQueue=new();
            visited[start]=true;componentQueue.Enqueue(start);
            while(componentQueue.Count>0)
            {
                int index=componentQueue.Dequeue();component.Add(index);
                int x=index%width,y=index/width;
                for(int n=0;n<8;n++)
                {
                    int nx=x+dx[n],ny=y+dy[n];
                    if(nx<0||nx>=width||ny<0||ny>=height)continue;
                    int next=ny*width+nx;
                    if(background[next]||visited[next])continue;
                    visited[next]=true;componentQueue.Enqueue(next);
                }
            }
            int componentMinX=width,componentMaxX=-1,componentMinY=height,componentMaxY=-1;
            foreach(int index in component)
            {
                int componentX=index%width,componentY=index/width;
                componentMinX=Mathf.Min(componentMinX,componentX);componentMaxX=Mathf.Max(componentMaxX,componentX);
                componentMinY=Mathf.Min(componentMinY,componentY);componentMaxY=Mathf.Max(componentMaxY,componentY);
            }
            bool thinLabelFragment=componentMaxY-componentMinY<=3&&componentMaxX-componentMinX>12;
            if(component.Count<12||thinLabelFragment)foreach(int index in component)background[index]=true;
        }

        if(frame.name.StartsWith("P2Idle"))
        {
            bool[] checkedPixels=new bool[crop.Length];
            List<List<int>> components=new();
            int largestCount=0;
            for(int start=0;start<crop.Length;start++)
            {
                if(background[start]||checkedPixels[start])continue;
                List<int> component=new();
                Queue<int> queue=new();
                checkedPixels[start]=true;queue.Enqueue(start);
                while(queue.Count>0)
                {
                    int index=queue.Dequeue();component.Add(index);
                    int x=index%width,y=index/width;
                    for(int n=0;n<8;n++)
                    {
                        int nx=x+dx[n],ny=y+dy[n];
                        if(nx<0||nx>=width||ny<0||ny>=height)continue;
                        int next=ny*width+nx;
                        if(background[next]||checkedPixels[next])continue;
                        checkedPixels[next]=true;queue.Enqueue(next);
                    }
                }
                components.Add(component);
                largestCount=Mathf.Max(largestCount,component.Count);
            }
            foreach(List<int> component in components)if(component.Count<largestCount)foreach(int index in component)background[index]=true;
        }

        if(frame.name.StartsWith("P2"))
        {
            bool[] checkedPixels=new bool[crop.Length];
            List<List<int>> components=new();
            int largestIndex=-1,largestCount=0;
            for(int start=0;start<crop.Length;start++)
            {
                if(background[start]||checkedPixels[start])continue;
                List<int> component=new();
                Queue<int> queue=new();
                checkedPixels[start]=true;queue.Enqueue(start);
                while(queue.Count>0)
                {
                    int index=queue.Dequeue();component.Add(index);
                    int x=index%width,y=index/width;
                    for(int n=0;n<8;n++)
                    {
                        int nx=x+dx[n],ny=y+dy[n];
                        if(nx<0||nx>=width||ny<0||ny>=height)continue;
                        int next=ny*width+nx;
                        if(background[next]||checkedPixels[next])continue;
                        checkedPixels[next]=true;queue.Enqueue(next);
                    }
                }
                components.Add(component);
                if(component.Count>largestCount){largestCount=component.Count;largestIndex=components.Count-1;}
            }
            for(int componentIndex=0;componentIndex<components.Count;componentIndex++)
            {
                if(componentIndex==largestIndex)continue;
                bool touchesSide=false;
                foreach(int index in components[componentIndex])
                {
                    int x=index%width;
                    if(x<=1||x>=width-2){touchesSide=true;break;}
                }
                if(touchesSide)foreach(int index in components[componentIndex])background[index]=true;
            }
        }

        int minX=width,maxX=-1,minY=height,maxY=-1;
        for(int y=0;y<height;y++)for(int x=0;x<width;x++)if(!background[y*width+x]){minX=Mathf.Min(minX,x);maxX=Mathf.Max(maxX,x);minY=Mathf.Min(minY,y);maxY=Mathf.Max(maxY,y);}
        if(maxX<minX||maxY<minY)return;

        int contentWidth=maxX-minX+1,contentHeight=maxY-minY+1;
        float maximumScale=frame.name.Contains("Projectile")?4f:1.35f;
        float scale=Mathf.Min(maximumScale,Mathf.Min(236f/contentWidth,236f/contentHeight));
        int drawWidth=Mathf.Max(1,Mathf.RoundToInt(contentWidth*scale)),drawHeight=Mathf.Max(1,Mathf.RoundToInt(contentHeight*scale));
        int offsetX=(size-drawWidth)/2,offsetY=8;
        Color32[] pixels=new Color32[size*size];
        for(int y=0;y<drawHeight;y++)for(int x=0;x<drawWidth;x++)
        {
            int sourceX=minX+Mathf.Clamp(Mathf.FloorToInt(x/scale),0,contentWidth-1);
            int sourceY=minY+Mathf.Clamp(Mathf.FloorToInt(y/scale),0,contentHeight-1);
            int sourceIndex=sourceY*width+sourceX;
            Color32 sampled=crop[sourceIndex];
            int sampledMax=Mathf.Max(sampled.r,Mathf.Max(sampled.g,sampled.b));
            int sampledMin=Mathf.Min(sampled.r,Mathf.Min(sampled.g,sampled.b));
            bool whiteArtifact=false;
            if(sampled.r>150&&sampled.g>150&&sampled.b>150&&sampledMax-sampledMin<30)
            {
                for(int checkY=-1;checkY<=1&&!whiteArtifact;checkY++)for(int checkX=-1;checkX<=1;checkX++)
                {
                    if(checkX==0&&checkY==0)continue;
                    int neighborX=sourceX+checkX,neighborY=sourceY+checkY;
                    if(neighborX<0||neighborX>=width||neighborY<0||neighborY>=height||background[neighborY*width+neighborX]){whiteArtifact=true;break;}
                }
            }
            bool enclosedWhite=sampled.r>175&&sampled.g>175&&sampled.b>175&&sampledMax-sampledMin<32;
            if(!background[sourceIndex]&&!whiteArtifact&&!enclosedWhite)pixels[(offsetY+y)*size+offsetX+x]=sampled;
        }

        Texture2D output=new(size,size,TextureFormat.RGBA32,false);
        output.SetPixels32(pixels);output.Apply();
        File.WriteAllBytes(Output+"/"+frame.name+".png",output.EncodeToPNG());
        Object.DestroyImmediate(output);
    }
private static void Configure(string path)
    {
        var importer=(TextureImporter)AssetImporter.GetAtPath(path);
        importer.textureType=TextureImporterType.Sprite;
        importer.spriteImportMode=SpriteImportMode.Single;
        importer.spritePixelsPerUnit=100;
        importer.filterMode=FilterMode.Point;
        importer.textureCompression=TextureImporterCompression.Uncompressed;
        importer.alphaIsTransparency=true;
        TextureImporterSettings settings=new();
        importer.ReadTextureSettings(settings);
        settings.spriteAlignment=(int)SpriteAlignment.Custom;
        settings.spritePivot=new Vector2(.5f,.04f);
        importer.SetTextureSettings(settings);
        importer.SaveAndReimport();
    }
private static void CreatePrefab(){GameObject root=new("AncientGolemBoss",typeof(SpriteRenderer),typeof(Rigidbody2D),typeof(CapsuleCollider2D),typeof(Damageable),typeof(BossHealth),typeof(BossMovement),typeof(AncientGolemAnimator),typeof(AncientGolemCombat));try{root.transform.localScale=Vector3.one*2f;var r=root.GetComponent<SpriteRenderer>();r.sprite=Load("P1Idle_01");r.sortingOrder=8;var body=root.GetComponent<Rigidbody2D>();body.gravityScale=0;body.freezeRotation=true;var col=root.GetComponent<CapsuleCollider2D>();col.size=new Vector2(1.15f,1.5f);col.offset=new Vector2(0,.45f);var so=new SerializedObject(root.GetComponent<AncientGolemAnimator>());Set(so,"phase1Idle","P1Idle",4);Set(so,"phase1Slam","P1Slam",5);Set(so,"phase1Throw","P1Throw",2);Set(so,"phase1Hit","P1Hit",2);Set(so,"phase1Death","P1Death",3);Set(so,"phase2Idle","P2Idle",4);Set(so,"phase2Slam","P2Slam",4);Set(so,"phase2Crack","P2Crack",3);Set(so,"phase2Volley","P2Volley",2);Set(so,"phase2Hit","P2Hit",4);Set(so,"phase2Death","P2Death",4);so.ApplyModifiedPropertiesWithoutUndo();PrefabUtility.SaveAsPrefabAsset(root,Prefab);}finally{Object.DestroyImmediate(root);}}
    private static void Set(SerializedObject so,string property,string prefix,int count){var array=so.FindProperty(property);array.arraySize=count;for(int n=0;n<count;n++)array.GetArrayElementAtIndex(n).objectReferenceValue=Load(prefix+"_"+(n+1).ToString("00"));}
    private static Sprite Load(string name)=>AssetDatabase.LoadAssetAtPath<Sprite>(Output+"/"+name+".png");
    private static void EnsureFolders(){if(!AssetDatabase.IsValidFolder("Assets/Resources"))AssetDatabase.CreateFolder("Assets","Resources");if(!AssetDatabase.IsValidFolder(Output))AssetDatabase.CreateFolder("Assets/Resources","AncientGolem");}
}
#endif
