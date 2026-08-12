namespace BlackFlag.Engine.Security.Containers;

/// <summary>Classifies embedded bytes without executing or activating them.</summary>
public interface IDocumentEmbeddedObjectClassifier
{
    DocumentEmbeddedObjectClassification Classify(DocumentEmbeddedObject embeddedObject);
}
