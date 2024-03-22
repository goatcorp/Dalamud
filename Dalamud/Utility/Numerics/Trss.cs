using System.Diagnostics.CodeAnalysis;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace Dalamud.Utility.Numerics;

/// <summary>Representation of a 3D transformation in 3 steps of translation, rotation, scale, and skew(X/Y),
/// or as a <see cref="Matrix4x4"/>.</summary>
[StructLayout(LayoutKind.Explicit, Size = 64)]
public struct Trss
{
    /// <summary>The Matrix4x4 view.</summary>
    [FieldOffset(0)]
    public Matrix4x4 Matrix;

    /// <summary>The 3D translation.</summary>
    [FieldOffset(0)]
    public Vector3 Translation3;

    /// <summary>The 2D translation part (X and Y) of <see cref="Translation3"/>.</summary>
    [FieldOffset(0)]
    public Vector2 Translation2;

    /// <summary>The 3D rotation.</summary>
    [FieldOffset(16)]
    public Quaternion Rotation;

    /// <summary>The 3D scale.</summary>
    [FieldOffset(32)]
    public Vector3 Scale3;

    /// <summary>The 2D scale part (X and Y) of <see cref="Scale3"/>.</summary>
    [FieldOffset(32)]
    public Vector2 Scale2;

    /// <summary>The 2D skew(shear).</summary>
    [FieldOffset(48)]
    public Vector2 SkewXY;

    /// <summary><c>0</c> if using TRSS; <c>1</c> if using <see cref="Matrix"/>.</summary>
    [FieldOffset(60)]
    public float ModeSpecifier;

    [FieldOffset(0)]
    private unsafe fixed float elements[16];

    /// <summary>Initializes a new instance of the <see cref="Trss"/> struct.</summary>
    /// <param name="translation2">The 2D translation component.</param>
    /// <param name="rotation">The rotation component.</param>
    /// <param name="scale2">The 2D scale component.</param>
    /// <param name="skewXy">The 2D skew(shear) component.</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public Trss(Vector2 translation2, Quaternion rotation, Vector2 scale2, Vector2 skewXy)
    {
        this.Translation2 = translation2;
        this.Rotation = rotation;
        this.Scale3 = new(scale2, 1f);
        this.SkewXY = skewXy;
    }

    /// <summary>Initializes a new instance of the <see cref="Trss"/> struct.</summary>
    /// <param name="translation3">The 3D translation component.</param>
    /// <param name="rotation">The rotation component.</param>
    /// <param name="scale3">The 3D scale component.</param>
    /// <param name="skewXy">The 2D skew(shear) component.</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public Trss(Vector3 translation3, Quaternion rotation, Vector3 scale3, Vector2 skewXy)
    {
        this.Translation3 = translation3;
        this.Rotation = rotation;
        this.Scale3 = scale3;
        this.SkewXY = skewXy;
    }

    /// <summary>Initializes a new instance of the <see cref="Trss"/> struct.</summary>
    /// <param name="matrix">A transformation matrix to decompose.</param>
    /// <remarks>If the matrix cannot be decomposed, <see cref="Identity"/> will be used instead.</remarks>
    public Trss(in Matrix4x4 matrix)
    {
        // Decompose returns false if matrix is not a TRSS matrix, in which case, we just store the matrix.
        if (!Matrix4x4.Decompose(matrix, out this.Scale3, out this.Rotation, out this.Translation3))
        {
            this.Matrix = matrix;
            this.ModeSpecifier = 1f;
        }
    }

    /// <summary>Initializes a new instance of the <see cref="Trss"/> struct.</summary>
    /// <param name="matrix">A transformation matrix to decompose.</param>
    /// <remarks>If the matrix cannot be decomposed, <see cref="Identity"/> will be used instead.</remarks>
    public Trss(in Matrix3x2 matrix)
    {
        // TODO: test this shit
        this.Scale3 = new(
            Vector2.SquareRoot(
                new(
                    (matrix.M11 * matrix.M11) + (matrix.M12 * matrix.M12),
                    (matrix.M21 * matrix.M21) + (matrix.M22 * matrix.M22))),
            1);
        var rad = MathF.Atan2(matrix.M12, matrix.M11);
        this.Rotation = Quaternion.CreateFromAxisAngle(Vector3.UnitZ, rad);
        this.Translation2 = new(matrix.M31, matrix.M32);
        this.SkewXY = new(
            0, // Ways to decompose a skew component is not unique. Assuming SkewX to be zero.
            MathF.Atan2(matrix.M22, matrix.M21) - (MathF.PI / 2f) - rad);
    }

    /// <summary>Gets the identity TRSS.</summary>
    public static Trss Identity
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => new(Vector3.Zero, Quaternion.Identity, Vector3.One, Vector2.Zero);
    }

    /// <summary>Gets a value indicating whether this TRSS is empty.</summary>
    public readonly bool IsIdentity =>
        this.ContainsRawMatrix
            ? this.Matrix.IsIdentity
            : this.Translation3 == Vector3.Zero
              && this.Rotation.IsIdentity
              && this.Scale3 == Vector3.One
              && this.SkewXY == Vector2.Zero;

    /// <summary>Gets a value indicating whether this instance directly contains a matrix.</summary>
    public readonly bool ContainsRawMatrix
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => this.ModeSpecifier != 0f;
    }

    /// <summary>Gets this TRSS as a <see cref="Matrix4x4"/>.</summary>
    [SuppressMessage("ReSharper", "InconsistentNaming", Justification = "no")]
    public readonly Matrix4x4 AsMatrix4x4 =>
        this.ModeSpecifier == 0f
            ? Matrix4x4.Multiply(
                Matrix4x4.CreateScale(this.Scale3),
                Matrix4x4.Multiply(
                    Matrix4x4.CreateFromQuaternion(this.Rotation),
                    Matrix4x4.CreateTranslation(this.Translation3)))
            : this.Matrix;

    /// <summary>Gets or sets the element at the given index.</summary>
    /// <param name="index">The index.</param>
    public unsafe float this[int index]
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        readonly get => index is >= 0 and < 16 ? this.elements[index] : throw new IndexOutOfRangeException();
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        set => this.elements[index] = index is >= 0 and < 16 ? value : throw new IndexOutOfRangeException();
    }

    /// <summary>Creates a TRSS that only has a translation component.</summary>
    /// <param name="v2">The 2D translation.</param>
    /// <returns>The initialized TRSS.</returns>
    public static Trss CreateTranslation(in Vector2 v2) =>
        new(v2, Quaternion.Identity, Vector2.One, Vector2.Zero);

    /// <summary>Creates a TRSS that only has a translation component.</summary>
    /// <param name="v3">The 3D translation.</param>
    /// <returns>The initialized TRSS.</returns>
    public static Trss CreateTranslation(in Vector3 v3) =>
        new(v3, Quaternion.Identity, Vector3.One, Vector2.Zero);
    
    /// <summary>Creates a TRSS that only has a rotation component.</summary>
    /// <param name="q">The quaternion representing the rotation.</param>
    /// <returns>The initialized TRSS.</returns>
    public static Trss CreateRotation(in Quaternion q) => new(Vector2.Zero, q, Vector2.One, Vector2.Zero);

    /// <summary>Creates a TRSS that only has a scale component.</summary>
    /// <param name="v2">The 2D scale.</param>
    /// <returns>The initialized TRSS.</returns>
    public static Trss CreateScale(in Vector2 v2) =>
        new(Vector2.Zero, Quaternion.Identity, v2, Vector2.Zero);

    /// <summary>Creates a TRSS that only has a scale component.</summary>
    /// <param name="v3">The 3D scale.</param>
    /// <returns>The initialized TRSS.</returns>
    public static Trss CreateScale(in Vector3 v3) =>
        new(Vector3.Zero, Quaternion.Identity, v3, Vector2.Zero);
    
    /// <summary>Creates a TRSS that only has a XY skew component.</summary>
    /// <param name="v2">The XY skew.</param>
    /// <returns>The initialized TRSS.</returns>
    public static Trss CreateSkew(in Vector2 v2) =>
        new(Vector2.Zero, Quaternion.Identity, Vector2.One, v2);

    /// <summary>Multiplies the given TRSes.</summary>
    /// <param name="trs1">The 1st TRSS to multiply.</param>
    /// <param name="trs2">The 2nd TRSS to multiply.</param>
    /// <returns>The multipled TRSS.</returns>
    public static Trss Multiply(in Trss trs1, in Trss trs2)
    {
        // TODO: this probably can be faster, but care later
        return new(Matrix4x4.Multiply(trs1.AsMatrix4x4, trs2.AsMatrix4x4));
    }

    /// <summary>Transforms <paramref name="v"/> by <paramref name="by"/>.</summary>
    /// <param name="v">A 2D vector to transform.</param>
    /// <param name="by">Transformation specifications.</param>
    /// <returns>The transformed 2D vector.</returns>
    public static Vector2 TransformVector(Vector2 v, in Trss by)
    {
        if (by.ContainsRawMatrix)
            return Vector2.Transform(v, by.Matrix);

        // effectively: Vector2.Transform(v, Matrix3x2.CreateSkew(by.SkewXY.X, by.SkewXY.Y));
        v = new(
            v.X + (v.Y * MathF.Tan(by.SkewXY.X)),
            v.Y + (MathF.Tan(by.SkewXY.Y) * v.X));

        v *= by.Scale2;
        v = Vector2.Transform(v, by.Rotation);
        v += by.Translation2;
        return v;
    }

    /// <summary>Transforms <paramref name="v"/> by <paramref name="by"/>.</summary>
    /// <param name="v">A 3D vector to transform.</param>
    /// <param name="by">Transformation specifications.</param>
    /// <returns>The transformed 3D vector.</returns>
    public static Vector3 TransformVector(Vector3 v, in Trss by)
    {
        if (by.ContainsRawMatrix)
            return Vector3.Transform(v, by.Matrix);

        // see above Vector2 variant
        v = new(
            v.X + (v.Y * MathF.Tan(by.SkewXY.X)),
            v.Y + (MathF.Tan(by.SkewXY.Y) * v.X),
            v.Z);

        v *= by.Scale3;
        v = Vector3.Transform(v, by.Rotation);
        v += by.Translation3;
        return v;
    }

    /// <summary>Inverse transforms <paramref name="v"/> by <paramref name="by"/>.</summary>
    /// <param name="v">A 2D vector to transform.</param>
    /// <param name="by">Transformation specifications.</param>
    /// <returns>The inverse transformed 2D vector.</returns>
    public static Vector2 TransformVectorInverse(Vector2 v, in Trss by)
    {
        v -= by.Translation2;
        v = Vector2.Transform(v, Quaternion.Inverse(by.Rotation));
        v /= by.Scale2;

        if (Matrix3x2.Invert(Matrix3x2.CreateSkew(by.SkewXY.X, by.SkewXY.Y), out var skewInvert))
            v = Vector2.Transform(v, skewInvert);
        return v;
    }

    /// <summary>Translates the given TRSS in 2D.</summary>
    /// <param name="transformation">The TRSS to translate.</param>
    /// <param name="translation">The 2D translation.</param>
    /// <returns>The translated TRSS.</returns>
    public static Trss Translate(in Trss transformation, Vector2 translation) =>
        new(transformation.Translation3 + new Vector3(translation, 0), transformation.Rotation, transformation.Scale3, transformation.SkewXY);

    /// <summary>Translates the given TRSS in 3D.</summary>
    /// <param name="transformation">The TRSS to translate.</param>
    /// <param name="translation">The 3D translation.</param>
    /// <returns>The translated TRSS.</returns>
    public static Trss Translate(in Trss transformation, Vector3 translation) =>
        new(transformation.Translation3 + translation, transformation.Rotation, transformation.Scale3, transformation.SkewXY);

    /// <summary>Removes the translation component from the given TRSS.</summary>
    /// <param name="transformation">The TRSS to remove component from.</param>
    /// <returns>The TRSS without translation.</returns>
    public static Trss WithoutTranslation(in Trss transformation)
    {
        if (transformation.ContainsRawMatrix)
            return new(transformation.Matrix with { M41 = 0, M42 = 0, M43 = 0 });
        return transformation with { Translation3 = Vector3.Zero };
    }
}
