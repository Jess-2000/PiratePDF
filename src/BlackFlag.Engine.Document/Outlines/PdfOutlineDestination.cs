namespace BlackFlag.Engine.Document.Outlines;

/// <summary>Identifies a direct logical outline destination by stable page identity.</summary>
public readonly record struct PdfOutlineDestination(Guid PageId);
