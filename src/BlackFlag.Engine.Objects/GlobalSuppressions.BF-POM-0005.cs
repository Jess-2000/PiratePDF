using System.Diagnostics.CodeAnalysis;

[assembly: SuppressMessage(
    "Naming",
    "CA1711:Identifiers should not have incorrect suffix",
    Justification = "PdfStream is the terminology defined by the PDF specification and is appropriate for the PDF object model.",
    Scope = "type",
    Target = "~T:BlackFlag.Engine.Objects.PdfStream")]

