using System;
using System.Text;
using BlackFlag.Engine.Security.Formats.Rtf;
using BlackFlag.Engine.Security.Model;

namespace BlackFlag.Engine.Security.Tests.Formats.Rtf;

public sealed class RtfSecurityAnalyzerTests
{
    [Fact]
    public void CleanRtfProducesNoObservations()
    {
        byte[] content = Encoding.ASCII.GetBytes(@"{\rtf1\ansi Clean text}");

        RtfSecurityAnalysisResult result =
            new RtfSecurityAnalyzer().Analyze(content, "clean.rtf");

        Assert.Empty(result.Observations);
    }

    [Fact]
    public void DdeFieldIsDetected()
    {
        byte[] content =
            Encoding.ASCII.GetBytes(
                @"{\rtf1{\field{\*\fldinst DDEAUTO harmless fixture}}}");

        RtfSecurityAnalysisResult result =
            new RtfSecurityAnalyzer().Analyze(content, "dde.rtf");

        Assert.Contains(
            result.Observations,
            item => item.Kind == "DdeField" &&
                    item.Category == DocumentSecurityObservationCategory.ActiveContent);
    }

    [Fact]
    public void EmbeddedPeObjectUsesSharedClassifier()
    {
        byte[] pe = new byte[128];
        pe[0] = (byte)'M';
        pe[1] = (byte)'Z';
        pe[0x3C] = 0x40;
        pe[0x40] = (byte)'P';
        pe[0x41] = (byte)'E';

        string hex = Convert.ToHexString(pe);
        byte[] content =
            Encoding.ASCII.GetBytes(
                $@"{{\rtf1{{\object{{\*\objdata {hex}}}}}}}");

        RtfSecurityAnalysisResult result =
            new RtfSecurityAnalyzer().Analyze(content, "embedded.rtf");

        Assert.Contains(
            result.Observations,
            item => item.Kind == "EmbeddedExecutableContent" &&
                    item.Category == DocumentSecurityObservationCategory.ExecutableContent);
    }
}
