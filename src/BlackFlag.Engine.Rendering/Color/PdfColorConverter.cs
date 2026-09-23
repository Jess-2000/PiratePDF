using BlackFlag.Engine.Objects;
using BlackFlag.Engine.Rendering.DisplayList;
using BlackFlag.Engine.Rendering.Resources;

namespace BlackFlag.Engine.Rendering.Color;

/// <summary>Provides deterministic PDF-to-sRGB preview conversion while preserving source semantics.</summary>
public sealed class PdfColorConverter : IPdfColorConverter
{
    /// <inheritdoc />
    public PdfSrgbColor Convert(PdfColorValue color, PdfColorTarget target, double alpha = 1)
    {
        ArgumentNullException.ThrowIfNull(color);
        if (target != PdfColorTarget.Srgb) throw new NotSupportedException($"Color target '{target}' is unsupported.");
        return ConvertCore(color, PdfSrgbColor.Clamp(alpha));
    }

    private static PdfSrgbColor ConvertCore(PdfColorValue color, double alpha)
    {
        IReadOnlyList<double> c=color.Components;
        if (color.Resource is PdfResolvedColorSpace r)
        {
            return r.ColorSpaceKind switch
            {
                PdfColorSpaceKind.CalGray => Gray(c,alpha),
                PdfColorSpaceKind.CalRgb => Rgb(c,alpha),
                PdfColorSpaceKind.Lab => Lab(c,alpha),
                PdfColorSpaceKind.Separation or PdfColorSpaceKind.DeviceN => Alternate(r,c,alpha),
                PdfColorSpaceKind.IccBased => Alternate(r,c,alpha),
                _ => ByName(color.ColorSpaceName,c,alpha)
            };
        }
        return ByName(color.ColorSpaceName,c,alpha);
    }

    private static PdfSrgbColor ByName(string name,IReadOnlyList<double> c,double a)=>name switch
    {
        "DeviceGray" or "G" when c.Count>=1 => Gray(c,a),
        "DeviceRGB" or "DeviceRgb" or "RGB" when c.Count>=3 => Rgb(c,a),
        "DeviceCMYK" or "DeviceCmyk" or "CMYK" when c.Count>=4 => Cmyk(c,a),
        _ => new(0,0,0,a)
    };
    private static PdfSrgbColor Gray(IReadOnlyList<double> c,double a){double g=PdfSrgbColor.Clamp(c.Count>0?c[0]:0);return new(g,g,g,a);}
    private static PdfSrgbColor Rgb(IReadOnlyList<double> c,double a)=>new(PdfSrgbColor.Clamp(c[0]),PdfSrgbColor.Clamp(c[1]),PdfSrgbColor.Clamp(c[2]),a);
    private static PdfSrgbColor Cmyk(IReadOnlyList<double> c,double a){double k=PdfSrgbColor.Clamp(c[3]);return new((1-PdfSrgbColor.Clamp(c[0]))*(1-k),(1-PdfSrgbColor.Clamp(c[1]))*(1-k),(1-PdfSrgbColor.Clamp(c[2]))*(1-k),a);}

    private static PdfSrgbColor Lab(IReadOnlyList<double> c,double a)
    {
        if (c.Count<3) return new(0,0,0,a);
        double l=Math.Clamp(c[0],0,100), aa=c[1], bb=c[2];
        double fy=(l+16)/116, fx=fy+aa/500, fz=fy-bb/200;
        static double F(double t){double t3=t*t*t;return t3>216d/24389d?t3:(116*t-16)/903.3;}
        double x=0.95047*F(fx), y=F(fy), z=1.08883*F(fz);
        double rl= 3.2404542*x-1.5371385*y-0.4985314*z;
        double gl=-0.9692660*x+1.8760108*y+0.0415560*z;
        double bl= 0.0556434*x-0.2040259*y+1.0572252*z;
        static double Gamma(double v)=>v<=0.0031308?12.92*v:1.055*Math.Pow(Math.Max(v,0),1/2.4)-0.055;
        return new(PdfSrgbColor.Clamp(Gamma(rl)),PdfSrgbColor.Clamp(Gamma(gl)),PdfSrgbColor.Clamp(Gamma(bl)),a);
    }

    private static PdfSrgbColor Alternate(PdfResolvedColorSpace r,IReadOnlyList<double> input,double a)
    {
        if (r.AlternateSpace is null) return new(0,0,0,a);
        double[] mapped=EvaluateType2(r.TintTransform,input);
        string name=r.AlternateSpace switch { PdfName n=>n.Value, PdfArray ar when ar.Count>0 && ar[0] is PdfName n=>n.Value, _=>"DeviceRGB"};
        return ByName(name,mapped,a);
    }

    private static double[] EvaluateType2(PdfObject? function,IReadOnlyList<double> input)
    {
        if (function is not PdfDictionary d || !d.TryGetValue(new PdfName("FunctionType"),out PdfObject ft) || ft is not PdfInteger i || i.Value!=2)
            return input.ToArray();
        double n=1; if(d.TryGetValue(new PdfName("N"),out PdfObject no)) n=Number(no);
        double x=Math.Pow(PdfSrgbColor.Clamp(input.Count>0?input[0]:0),n);
        double[] c0=Array(d,"C0",[0]); double[] c1=Array(d,"C1",[1]); int count=Math.Max(c0.Length,c1.Length); double[] o=new double[count];
        for(int j=0;j<count;j++){double v0=j<c0.Length?c0[j]:c0[^1],v1=j<c1.Length?c1[j]:c1[^1];o[j]=v0+x*(v1-v0);} return o;
    }
    private static double[] Array(PdfDictionary d,string key,double[] fallback)=>d.TryGetValue(new PdfName(key),out PdfObject o)&&o is PdfArray a?a.Select(Number).ToArray():fallback;
    private static double Number(PdfObject o)=>o switch{PdfInteger i=>i.Value,PdfReal r=>r.Value,_=>throw new InvalidDataException("Color function contains a non-number.")};
}
