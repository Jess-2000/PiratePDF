namespace BlackFlag.Engine.Services.Resources;

/// <summary>Describes how a page-local resource mutation obtains its effective resources.</summary>
public enum PdfResourceMutationOwnership
{
    DirectPageResources = 0,
    IndirectPageResourcesCopyOnWrite = 1,
    InheritedResourcesCopyOnWrite = 2,
    NewPageResources = 3
}
