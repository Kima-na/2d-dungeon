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
        new("P1Idle_01",370,60,95,135),new("P1Idle_02",465,60,95,135),new("P1Idle_03",560,60,95,135),new("P1Idle_04",655,60,95,135),new("P1Idle_05",750,60,105,135),
        new("P1Slam_01",875,60,120,155),new("P1Slam_02",995,60,120,155),new("P1Slam_03",1115,60,125,155),new("P1Slam_04",1240,60,130,155),new("P1Slam_05",1370,60,145,155),
        new("P1Throw_01",380,245,110,135),new("P1Throw_02",490,245,115,135),new("P1Hit_01",845,250,115,140),new("P1Hit_02",960,250,125,140),
        new("P1Death_01",1110,250,100,145),new("P1Death_02",1210,250,100,145),new("P1Death_03",1310,250,100,145),new("P1Death_04",1410,250,105,145),
        new("P2Idle_01",380,465,100,150),new("P2Idle_02",480,465,100,150),new("P2Idle_03",580,465,100,150),new("P2Idle_04",680,465,100,150),new("P2Idle_05",780,465,105,150),
        new("P2Slam_01",950,470,135,165),new("P2Slam_02",1085,470,135,165),new("P2Slam_03",1220,470,140,165),new("P2Slam_04",1360,470,155,165),
        new("P2Crack_01",380,650,190,145),new("P2Crack_02",570,650,190,145),new("P2Crack_03",760,650,190,145),
        new("P2Volley_01",950,650,125,150),new("P2Volley_02",1075,650,130,150),
        new("P2Hit_01",380,850,120,160),new("P2Hit_02",500,850,120,160),new("P2Hit_03",620,850,120,160),new("P2Hit_04",740,850,120,160),
        new("P2Death_01",900,850,150,170),new("P2Death_02",1050,850,150,170),new("P2Death_03",1200,850,150,170),new("P2Death_04",1350,850,165,170)};

    [MenuItem("Tools/2D Dungeon/Setup Ancient Golem Boss")]
    public static void Setup(){ var importer=AssetImporter.GetAtPath(Source) as TextureImporter; if(importer==null){Debug.LogError("Ancient Golem source missing");return;} bool readable=importer.isReadable; importer.isReadable=true; importer.textureCompression=TextureImporterCompression.Uncompressed; importer.SaveAndReimport(); EnsureFolders(); Texture2D source=AssetDatabase.LoadAssetAtPath<Texture2D>(Source); foreach(var frame in Frames) Write(source,frame); AssetDatabase.Refresh(); foreach(var frame in Frames) Configure(Output+"/"+frame.name+".png"); CreatePrefab(); importer=(TextureImporter)AssetImporter.GetAtPath(Source); importer.isReadable=readable; importer.SaveAndReimport(); AssetDatabase.SaveAssets(); Debug.Log("ANCIENT_GOLEM_SETUP_PASS: "+Frames.Length+" frames and HARD boss prefab created."); }
    private static void Write(Texture2D source,Frame frame){ const int size=256; Color32[] pixels=new Color32[size*size]; var visible=new List<(int x,int y,Color32 c)>(); for(int y=0;y<frame.rect.height;y++)for(int x=0;x<frame.rect.width;x++){ int sx=frame.rect.x+x, sy=source.height-frame.rect.y-frame.rect.height+y; if(sx<0||sx>=source.width||sy<0||sy>=source.height)continue; Color32 c=source.GetPixel(sx,sy); int bright=Mathf.Max(c.r,Mathf.Max(c.g,c.b)); int chroma=bright-Mathf.Min(c.r,Mathf.Min(c.g,c.b)); bool blueBackdrop=c.b>c.r*1.45f&&c.b>c.g*1.2f&&c.r<85&&c.g<155; if(bright<66 || (bright<118&&chroma<22) || blueBackdrop)continue; visible.Add((x,y,c)); } if(visible.Count==0)return; int minX=9999,maxX=0,minY=9999,maxY=0; foreach(var p in visible){minX=Mathf.Min(minX,p.x);maxX=Mathf.Max(maxX,p.x);minY=Mathf.Min(minY,p.y);maxY=Mathf.Max(maxY,p.y);} float scale=Mathf.Min(1.35f,Mathf.Min(236f/(maxX-minX+1),236f/(maxY-minY+1))); int ox=(size-Mathf.RoundToInt((maxX-minX+1)*scale))/2,oy=8; foreach(var p in visible){int px=ox+Mathf.RoundToInt((p.x-minX)*scale),py=oy+Mathf.RoundToInt((p.y-minY)*scale); if(px>=0&&px<size&&py>=0&&py<size)pixels[py*size+px]=p.c;} Texture2D output=new(size,size,TextureFormat.RGBA32,false);output.SetPixels32(pixels);output.Apply();File.WriteAllBytes(Output+"/"+frame.name+".png",output.EncodeToPNG());Object.DestroyImmediate(output); }
    private static void Configure(string path){var i=(TextureImporter)AssetImporter.GetAtPath(path);i.textureType=TextureImporterType.Sprite;i.spriteImportMode=SpriteImportMode.Single;i.spritePixelsPerUnit=100;i.filterMode=FilterMode.Point;i.textureCompression=TextureImporterCompression.Uncompressed;i.alphaIsTransparency=true;i.spritePivot=new Vector2(.5f,.04f);i.SaveAndReimport();}
    private static void CreatePrefab(){GameObject root=new("AncientGolemBoss",typeof(SpriteRenderer),typeof(Rigidbody2D),typeof(CapsuleCollider2D),typeof(Damageable),typeof(BossHealth),typeof(BossMovement),typeof(AncientGolemAnimator),typeof(AncientGolemCombat));try{root.transform.localScale=Vector3.one*1.25f;var r=root.GetComponent<SpriteRenderer>();r.sprite=Load("P1Idle_01");r.sortingOrder=8;var body=root.GetComponent<Rigidbody2D>();body.gravityScale=0;body.freezeRotation=true;var col=root.GetComponent<CapsuleCollider2D>();col.size=new Vector2(1.15f,1.5f);col.offset=new Vector2(0,.45f);var so=new SerializedObject(root.GetComponent<AncientGolemAnimator>());Set(so,"phase1Idle","P1Idle",5);Set(so,"phase1Slam","P1Slam",5);Set(so,"phase1Throw","P1Throw",2);Set(so,"phase1Hit","P1Hit",2);Set(so,"phase1Death","P1Death",4);Set(so,"phase2Idle","P2Idle",5);Set(so,"phase2Slam","P2Slam",4);Set(so,"phase2Crack","P2Crack",3);Set(so,"phase2Volley","P2Volley",2);Set(so,"phase2Hit","P2Hit",4);Set(so,"phase2Death","P2Death",4);so.ApplyModifiedPropertiesWithoutUndo();PrefabUtility.SaveAsPrefabAsset(root,Prefab);}finally{Object.DestroyImmediate(root);}}
    private static void Set(SerializedObject so,string property,string prefix,int count){var array=so.FindProperty(property);array.arraySize=count;for(int n=0;n<count;n++)array.GetArrayElementAtIndex(n).objectReferenceValue=Load(prefix+"_"+(n+1).ToString("00"));}
    private static Sprite Load(string name)=>AssetDatabase.LoadAssetAtPath<Sprite>(Output+"/"+name+".png");
    private static void EnsureFolders(){if(!AssetDatabase.IsValidFolder("Assets/Resources"))AssetDatabase.CreateFolder("Assets","Resources");if(!AssetDatabase.IsValidFolder(Output))AssetDatabase.CreateFolder("Assets/Resources","AncientGolem");}
}
#endif
