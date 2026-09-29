$ErrorActionPreference = 'Stop'
Add-Type -AssemblyName System.Drawing
Add-Type -ReferencedAssemblies System.Drawing.Common,System.Drawing.Primitives,System.Private.Windows.GdiPlus,System.Private.Windows.Core -TypeDefinition @'
using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.Drawing.Drawing2D;
public static class MainSkinTextureImport
{
    // Generated RGB is fitted to the original UV canvas. Original alpha, PMA
    // edges and untouched animation/effect regions are retained byte-for-byte.
    public static void Fit(string source, string generated, string output, string mode)
    {
        using(var original=new Bitmap(source))
        using(var art=new Bitmap(generated))
        using(var fitted=new Bitmap(original.Width,original.Height,PixelFormat.Format32bppArgb))
        using(var result=new Bitmap(original.Width,original.Height,PixelFormat.Format32bppArgb))
        {
            using(var g=Graphics.FromImage(fitted))
            {
                g.InterpolationMode=InterpolationMode.HighQualityBicubic;
                var crop=mode=="top"?new Rectangle(0,204,art.Width,305):new Rectangle(0,0,art.Width,mode=="board"?1026:art.Height);
                g.DrawImage(art,new Rectangle(0,0,fitted.Width,fitted.Height),crop,GraphicsUnit.Pixel);
            }
            for(int y=0;y<original.Height;y++) for(int x=0;x<original.Width;x++)
            {
                Color a=original.GetPixel(x,y), b=fitted.GetPixel(x,y);
                if(mode.EndsWith("blur"))
                {
                    int red=0,green=0,blue=0,count=0;
                    for(int k=-12;k<=12;k++)
                    {
                        var sample=fitted.GetPixel(x,Math.Max(0,Math.Min(fitted.Height-1,y+k)));
                        red+=sample.R;green+=sample.G;blue+=sample.B;count++;
                    }
                    b=Color.FromArgb(red/count,green/count,blue/count);
                }
                bool selected=true, chroma=false, pma=false;
                if(mode=="dragon") {
                    selected=(new Rectangle(2,110,366,505).Contains(x,y)||new Rectangle(370,125,306,490).Contains(x,y)||
                        new Rectangle(2,617,458,602).Contains(x,y)||new Rectangle(2,1221,392,668).Contains(x,y)||
                        new Rectangle(1118,11,262,209).Contains(x,y)||new Rectangle(1120,254,236,258).Contains(x,y)||
                        new Rectangle(1405,1346,202,128).Contains(x,y)||new Rectangle(396,1254,386,217).Contains(x,y)||
                        new Rectangle(1384,1588,144,301).Contains(x,y)||new Rectangle(462,914,165,338).Contains(x,y)||
                        new Rectangle(783,1505,286,384).Contains(x,y)) && a.GetSaturation()>.35f && a.R>a.B*1.3f;
                    chroma=true;pma=true;
                }
                if(mode=="spin") { selected=x<185 && y>120 && a.G>a.R*1.1f;chroma=true;pma=true; }
                if(mode=="bank" || mode=="treasure") { selected=a.B>a.G*1.15f && a.R>a.G*1.05f;chroma=true;pma=true; }
                if(mode=="grand") { selected=x<404&&y>262&&y<385;pma=true; }
                if(mode=="major"||mode=="minor") { selected=x<322&&y>159&&y<259;pma=true; }
                if(!selected || (mode!="background" && a.A==0)) { result.SetPixel(x,y,a);continue; }
                if(chroma)
                {
                    // Preserve original shading and all lettering/mesh seam detail.
                    if(b.GetSaturation()<.25f){result.SetPixel(x,y,a);continue;}
                    float v=Math.Max(a.R,Math.Max(a.G,a.B));
                    float m=Math.Max(1,(int)Math.Max(b.R,Math.Max(b.G,b.B)));
                    result.SetPixel(x,y,Color.FromArgb(a.A,(int)(v*b.R/m),(int)(v*b.G/m),(int)(v*b.B/m)));
                }
                else
                {
                    if(pma && b.GetSaturation()<.13f && Math.Max(b.R,Math.Max(b.G,b.B))>80){result.SetPixel(x,y,a);continue;}
                    float factor=pma?a.A/255f:1;
                    result.SetPixel(x,y,Color.FromArgb(mode=="background"?255:a.A,(int)(b.R*factor),(int)(b.G*factor),(int)(b.B*factor)));
                }
            }
            result.Save(output,ImageFormat.Png);
        }
    }
}
'@
$skinRoot = Split-Path (Split-Path $PSScriptRoot -Parent) -Parent
$manifest = Get-Content -LiteralPath "$PSScriptRoot/textures.json" -Raw | ConvertFrom-Json
foreach ($item in $manifest) {
    $destination = Join-Path $skinRoot $item.output
    New-Item -ItemType Directory -Force (Split-Path $destination -Parent) | Out-Null
    [MainSkinTextureImport]::Fit((Join-Path $skinRoot $item.source),$item.generated,$destination,$item.mode)
    Write-Output $item.output
}


