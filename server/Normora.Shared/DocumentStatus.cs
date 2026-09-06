namespace Normora.Shared;

/// <summary>
/// Represents the processing lifecycle state of a document within the system.
/// </summary>
public enum DocumentStatus
{
    /// <summary>
    /// The document has been uploaded to object storage and metadata created, but processing has not started.
    /// </summary>
    Uploaded,

    /// <summary>
    /// The document is currently being processed (e.g., text extraction, chunking, embedding).
    /// </summary>
    Processing,

    /// <summary>
    /// Processing completed successfully. The document is fully indexed and ready for retrieval.
    /// </summary>
    Ready,

    /// <summary>
    /// Processing failed due to an error (e.g., corrupted file, extraction failure).
    /// </summary>
    Failed
}
