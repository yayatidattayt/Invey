namespace Pocket.Core;
public static class TesseractGeometry
{
    public static IReadOnlyList<(int A,int B)> Edges {get;} = Array.AsReadOnly(
        (from a in Enumerable.Range(0,16) from bit in Enumerable.Range(0,4)
        let b=a^(1<<bit) where a<b select (a,b)).ToArray());
    public static (double X,double Y)[] Project(double yaw,double pitch)
    {
        if(!double.IsFinite(yaw)||!double.IsFinite(pitch))throw new ArgumentOutOfRangeException(nameof(yaw));
        return Enumerable.Range(0,16).Select(i=>{
            double w=(i&8)==0?-1:1, f=3/(3-w);
            double x=((i&1)==0?-1:1)*f,y=((i&2)==0?-1:1)*f,z=((i&4)==0?-1:1)*f;
            double rx=x*Math.Cos(yaw)+z*Math.Sin(yaw),rz=-x*Math.Sin(yaw)+z*Math.Cos(yaw);
            double ry=y*Math.Cos(pitch)-rz*Math.Sin(pitch),depth=y*Math.Sin(pitch)+rz*Math.Cos(pitch);
            double perspective=6/(6-depth);
            return (rx*perspective,ry*perspective);
        }).ToArray();
    }
}
