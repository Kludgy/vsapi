namespace Vintagestory.API.MathTools;

public static class MathExtensions
{
    public static FastVec3d ToFastVec3d(this Vec3d a)
    {
        return new FastVec3d(a.X, a.Y, a.Z);
    }

    public static FastVec3d ToFastVec3d(this Vec3f a)
    {
        return new FastVec3d(a.X, a.Y, a.Z);
    }

    public static FastVec3d MulCopy(this FastVec3d a, double x, double y, double z)
    {
        return new FastVec3d(a.X * x, a.Y * y, a.Z * z);
    }

    public static FastVec3d SubCopy(this FastVec3d a, FastVec3d b)
    {
        return new FastVec3d(a.X - b.X, a.Y - b.Y, a.Z - b.Z);
    }

    public static void CopyIntoVec3d(this Vec3d a, ref Vec3d b)
    {
        if (b == null)
        {
            b = new Vec3d(a.X, a.Y, a.Z);
        }
        else
        {
            b.X = a.X;
            b.Y = a.Y;
            b.Z = a.Z;
        }
    }

    public static void CopyIntoVec3d(this FastVec3d a, ref Vec3d b)
    {
        if (b == null)
        {
            b = new Vec3d(a.X, a.Y, a.Z);
        }
        else
        {
            b.X = a.X;
            b.Y = a.Y;
            b.Z = a.Z;
        }
    }
}
