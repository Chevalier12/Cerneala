using System.Numerics;
using Cerneala.Drawing;
using Cerneala.Drawing.Prism;
using Cerneala.Drawing.Prism.Catalog;
using Cerneala.Drawing.Prism.ColorManagement;
using Cerneala.Drawing.Prism.Filters;
using Cerneala.Drawing.Prism.Graph;
using Cerneala.UI.Prism.Definitions;
using Cerneala.UI.Prism.Runtime;

namespace Cerneala.Tests.Drawing.Prism;

public sealed class PrismColorMatrixFilterTests
{
    [Fact]
    public void PlannerExposesOptionalTypedResourceAndPacksIdentity()
    {
        PrismCatalogFilterPlan plan = CreatePlan(resourceId: default);
        PrismCatalogPropertyDescriptor matrix =
            PrismCatalogRuntime.GetEntry((int)PrismFilterId.ColorMatrix)
                .Properties
                .Single(property => property.Name == "Matrix");

        Assert.Equal(PrismCatalogValueType.Resource, matrix.ValueType);
        Assert.False(matrix.Required);
        Assert.Equal(default(PrismResourceId), plan.PrimaryResource);
        Assert.False(plan.PrimaryResourceRequired);
        Assert.Equal(1, plan.Options0.X);
        Assert.Equal(Vector4.UnitX, plan.Options2);
        Assert.Equal(Vector4.UnitY, plan.Options3);
        Assert.Equal(Vector4.UnitZ, plan.Options4);
        Assert.Equal(new Vector4(0, 0, 0, 1), plan.Options5);
        Assert.Equal(Vector4.Zero, plan.Options6);
    }

    [Fact]
    public void CpuPathAppliesAffineRgbaRowsToStraightColor()
    {
        PrismColorMatrixResource matrix = new(
            new Matrix4x4(
                0, 1, 0, 0,
                1, 0, 0, 0,
                0, 0, 0.5f, 0.25f,
                0, 0, 0, 0.5f),
            new Vector4(0, 0.1f, 0.05f, 0.1f));
        PrismPremultipliedColor source =
            PrismPremultipliedColor.FromStraight(
                0.2,
                0.4,
                0.6,
                0.5);

        PrismPremultipliedColor result = Assert.Single(
            PrismCatalogFilterMath.Apply(
                CreatePlan(MatrixResource),
                [source],
                1,
                1,
                PrismColorProfile.LinearSrgb,
                colorMatrixResource: matrix));

        Assert.Equal(0.14, result.Red, 5);
        Assert.Equal(0.105, result.Green, 5);
        Assert.Equal(0.16625, result.Blue, 5);
        Assert.Equal(0.35, result.Alpha, 5);
    }

    [Fact]
    public void ClampFalsePreservesExtendedRgbAndStillBoundsAlpha()
    {
        PrismColorMatrixResource matrix = new(
            Matrix4x4.Identity,
            new Vector4(0.6f, -0.4f, 0, 2));
        PrismPremultipliedColor source =
            PrismPremultipliedColor.FromStraight(
                0.8,
                0.2,
                0.4,
                1);

        PrismPremultipliedColor result = Assert.Single(
            PrismCatalogFilterMath.Apply(
                CreatePlan(MatrixResource, clamp: false),
                [source],
                1,
                1,
                PrismColorProfile.LinearSrgb,
                colorMatrixResource: matrix));

        Assert.Equal(1.4, result.Red, 5);
        Assert.Equal(-0.2, result.Green, 5);
        Assert.Equal(0.4, result.Blue, 5);
        Assert.Equal(1, result.Alpha, 5);
    }

    [Fact]
    public void ResourceRejectsNonFiniteCoefficientsAndOffsets()
    {
        Matrix4x4 invalidMatrix = Matrix4x4.Identity;
        invalidMatrix.M23 = float.NaN;

        Assert.Throws<ArgumentOutOfRangeException>(
            () => new PrismColorMatrixResource(
                invalidMatrix,
                Vector4.Zero));
        Assert.Throws<ArgumentOutOfRangeException>(
            () => new PrismColorMatrixResource(
                Matrix4x4.Identity,
                new Vector4(0, 0, float.PositiveInfinity, 0)));
    }

    [Fact]
    public void DrawResourcesPreserveMatrixIdentityVersionAndDependency()
    {
        PrismColorMatrixResource matrix = new(
            Matrix4x4.Identity,
            Vector4.Zero);
        PrismDrawResources resources = PrismDrawResources.Create(
            [],
            [],
            [],
            [],
            [],
            [new PrismDrawColorMatrixResource(
                MatrixResource,
                matrix,
                17,
                29)]);

        Assert.True(resources.TryGetColorMatrix(
            MatrixResource,
            out PrismColorMatrixResource? resolved,
            out long identity,
            out long version));
        Assert.Same(matrix, resolved);
        Assert.Equal(17, version);
        Assert.Equal(29, identity);
        Assert.True(resources.TryGetDependency(
            MatrixResource,
            out long dependencyIdentity,
            out long dependencyVersion));
        Assert.Equal(29, dependencyIdentity);
        Assert.Equal(17, dependencyVersion);
    }

    private static readonly PrismResourceId MatrixResource =
        new("color-matrix");

    private static PrismCatalogFilterPlan CreatePlan(
        PrismResourceId resourceId,
        bool clamp = true) =>
        PrismCatalogFilterPlanner.Create(
            PrismFilterId.ColorMatrix,
            [
                new PrismGraphParameter(
                    0,
                    PrismGraphParameterValueKind.Boolean,
                    booleanValue: clamp),
                new PrismGraphParameter(
                    1,
                    PrismGraphParameterValueKind.Resource,
                    resourceValue: resourceId)
            ],
            PrismBlendMode.Normal,
            1,
            Matrix3x2.Identity,
            new DrawRect(0, 0, 1, 1));
}
