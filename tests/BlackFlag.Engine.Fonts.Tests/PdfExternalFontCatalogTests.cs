#nullable enable

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using BlackFlag.Engine.Fonts;
using BlackFlag.Engine.Fonts.Sanitization;
using Xunit;

namespace BlackFlag.Engine.Fonts.Tests;

public sealed class PdfExternalFontCatalogTests
{
    [Fact]
    public void CatalogSnapshotsProvidersAndOrdersFacesDeterministically()
    {
        var providers =
            new List<IPdfExternalFontProvider>
            {
                new FakeProvider(
                    "z-provider",
                    [
                        Face(
                            "z-provider",
                            "2",
                            "Beta",
                            "Beta Regular")
                    ]),
                new FakeProvider(
                    "a-provider",
                    [
                        Face(
                            "a-provider",
                            "1",
                            "Alpha",
                            "Alpha Regular")
                    ])
            };

        var catalog =
            new PdfExternalFontCatalog(
                providers);

        providers.Clear();

        IReadOnlyList<PdfExternalFontFace> faces =
            catalog.GetFaces();

        Assert.Equal(2, faces.Count);
        Assert.Equal("Alpha", faces[0].FamilyName);
        Assert.Equal("Beta", faces[1].FamilyName);
        Assert.Equal(2, catalog.Providers.Count);
    }

    [Fact]
    public void MatchRequiresGlyphCoverageAndConservativeEmbeddingRights()
    {
        var provider =
            new FakeProvider(
                "provider",
                [
                    Face(
                        "provider",
                        "allowed",
                        "Test Sans",
                        "Test Sans Regular",
                        rights: PdfExternalFontEmbeddingRights.Allowed),
                    Face(
                        "provider",
                        "restricted",
                        "Test Sans",
                        "Test Sans Restricted",
                        rights: PdfExternalFontEmbeddingRights.Restricted)
                ],
                unsupported:
                [
                    ("allowed", new Rune('\u03A9'))
                ]);

        var catalog =
            new PdfExternalFontCatalog(
                [provider]);

        Assert.Empty(
            catalog.Match(
                new PdfExternalFontMatchRequest(
                    "Test Sans"),
                "A\u03A9"));

        IReadOnlyList<PdfExternalFontMatch> matches =
            catalog.Match(
                new PdfExternalFontMatchRequest(
                    "Test Sans"),
                "ABC");

        PdfExternalFontMatch match =
            Assert.Single(matches);

        Assert.Equal("allowed", match.Face.FaceId);
    }

    [Fact]
    public void MatchOrdersExactSlantThenWeightThenStretch()
    {
        var provider =
            new FakeProvider(
                "provider",
                [
                    Face(
                        "provider",
                        "regular",
                        "Test Sans",
                        "Regular",
                        weight: 400,
                        slant: PdfExternalFontSlant.Upright),
                    Face(
                        "provider",
                        "italic",
                        "Test Sans",
                        "Italic",
                        weight: 400,
                        slant: PdfExternalFontSlant.Italic),
                    Face(
                        "provider",
                        "bold-italic",
                        "Test Sans",
                        "Bold Italic",
                        weight: 700,
                        slant: PdfExternalFontSlant.Italic)
                ]);

        var catalog =
            new PdfExternalFontCatalog(
                [provider]);

        IReadOnlyList<PdfExternalFontMatch> matches =
            catalog.Match(
                new PdfExternalFontMatchRequest(
                    "Test Sans",
                    weight: 650,
                    slant: PdfExternalFontSlant.Italic),
                "Hello");

        Assert.Equal(
            "bold-italic",
            matches[0].Face.FaceId);
        Assert.Equal(
            "italic",
            matches[1].Face.FaceId);
        Assert.Equal(
            "regular",
            matches[2].Face.FaceId);
    }

    [Fact]
    public void UnknownEmbeddingRightsAreRejectedByDefault()
    {
        var provider =
            new FakeProvider(
                "provider",
                [
                    Face(
                        "provider",
                        "unknown",
                        "Test Sans",
                        "Unknown",
                        rights: PdfExternalFontEmbeddingRights.Unknown)
                ]);

        var catalog =
            new PdfExternalFontCatalog(
                [provider]);

        Assert.Empty(
            catalog.Match(
                new PdfExternalFontMatchRequest(
                    "Test Sans"),
                "ABC"));

        Assert.Single(
            catalog.Match(
                new PdfExternalFontMatchRequest(
                    "Test Sans",
                    requireEmbeddable: false),
                "ABC"));
    }

    [Fact]
    public void DuplicateProviderIdsAreRejected()
    {
        Assert.Throws<ArgumentException>(
            () =>
                new PdfExternalFontCatalog(
                [
                    new FakeProvider("same", []),
                    new FakeProvider("same", [])
                ]));
    }

    [Fact]
    public void FontProgramIsExposedOnlyThroughSanitizerGateway()
    {
        var provider =
            new FakeProvider(
                "provider",
                [
                    Face(
                        "provider",
                        "face",
                        "Test Sans",
                        "Regular")
                ],
                program:
                    [1, 2, 3, 4]);

        var catalog =
            new PdfExternalFontCatalog(
                [provider]);

        PdfExternalFontMatch match =
            Assert.Single(
                catalog.Match(
                    new PdfExternalFontMatchRequest(
                        "Test Sans"),
                    "A"));

        var sanitizer =
            new CapturingSanitizer();

        _ = match.SanitizeFontProgram(
            sanitizer);

        Assert.Equal(
            [1, 2, 3, 4],
            sanitizer.ObservedBytes);
    }

    private sealed class CapturingSanitizer :
        IPdfExternalFontSanitizer
    {
        public byte[] ObservedBytes { get; private set; } = [];

        public PdfSanitizedExternalFontProgram Sanitize(
            Stream untrustedFontProgram,
            PdfExternalFontSanitizationOptions? options = null,
            CancellationToken cancellationToken = default)
        {
            using var memory =
                new MemoryStream();

            untrustedFontProgram.CopyTo(memory);
            ObservedBytes = memory.ToArray();

            // The real sanitizer is covered separately. This test proves the
            // matched-font API cannot return provider bytes directly.
            using var minimal =
                TestFontFactory.CreateMinimalTrueType();

            return new PdfOpenTypeFontSanitizer().Sanitize(
                minimal,
                options,
                cancellationToken);
        }
    }

    private static PdfExternalFontFace Face(
        string providerId,
        string faceId,
        string familyName,
        string faceName,
        int weight = 400,
        int stretch = 5,
        PdfExternalFontSlant slant = PdfExternalFontSlant.Upright,
        PdfExternalFontEmbeddingRights rights = PdfExternalFontEmbeddingRights.Allowed) =>
        new(
            providerId,
            faceId,
            familyName,
            faceName,
            weight,
            stretch,
            slant,
            PdfExternalFontTechnology.TrueType,
            rights);

    private sealed class FakeProvider(
        string id,
        IReadOnlyList<PdfExternalFontFace> faces,
        IEnumerable<(string FaceId, Rune Rune)>? unsupported = null,
        byte[]? program = null) :
        IPdfExternalFontProvider
    {
        private readonly IReadOnlyList<PdfExternalFontFace> faces = faces;
        private readonly HashSet<(string FaceId, int Rune)> unsupported =
                unsupported?
                    .Select(
                        static item =>
                            (item.FaceId, item.Rune.Value))
                    .ToHashSet()
                ?? [];
        private readonly byte[] program =
                program?.ToArray()
                ?? [0x00, 0x01, 0x00, 0x00];

        public string Id { get; } = id;

        public IReadOnlyList<PdfExternalFontFace> GetFaces(
            CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            return faces;
        }

        public bool SupportsRune(
            PdfExternalFontFace face,
            Rune rune)
        {
            ArgumentNullException.ThrowIfNull(face);

            if (!string.Equals(
                    face.ProviderId,
                    Id,
                    StringComparison.Ordinal))
            {
                throw new ArgumentException(
                    "The face belongs to another provider.",
                    nameof(face));
            }

            return !unsupported.Contains(
                (face.FaceId, rune.Value));
        }

        public Stream OpenFontProgram(
            PdfExternalFontFace face)
        {
            ArgumentNullException.ThrowIfNull(face);

            return new MemoryStream(
                program,
                writable: false);
        }
    }
}
