using System.Collections.Generic;
using BlackFlag.Engine.Security.Model;

namespace BlackFlag.Engine.Security.Tests.Model;

public sealed class DocumentSecurityObservationTests
{
    [Fact]
    public void PropertiesAreCopiedOnConstruction()
    {
        var source =
            new Dictionary<string, string>
            {
                ["Language"] = "JavaScript"
            };

        var observation =
            new DocumentSecurityObservation(
                DocumentSecurityObservationCategory.ScriptContent,
                "JavaScript",
                "The PDF contains JavaScript.",
                "pdf",
                properties: source);

        source["Language"] = "Changed";

        Assert.Equal(
            "JavaScript",
            observation.Properties["Language"]);
    }

    [Fact]
    public void SourceLocationCanRepresentContainerOrPdfLocations()
    {
        var location =
            new DocumentSecuritySourceLocation
            {
                PageIndex = 2,
                LogicalPath = "/word/vbaProject.bin",
                ByteOffset = 4096,
                ObjectIdentifier = "12 0"
            };

        Assert.Equal(2, location.PageIndex);
        Assert.Equal("/word/vbaProject.bin", location.LogicalPath);
        Assert.Equal(4096, location.ByteOffset);
        Assert.Equal("12 0", location.ObjectIdentifier);
    }
}
