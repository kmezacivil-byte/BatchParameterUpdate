namespace BatchParameterUpdate.Core;

/// <summary>
/// Why a selected element was not updated. Each value maps to a human-readable
/// message built by <see cref="ElementOutcome"/>; see README > Design decisions
/// for the full case-by-case rationale.
/// </summary>
public enum SkipReason
{
    /// <summary>The element has no type-instance model geometry to carry an
    /// instance parameter (e.g. the selection contains an ElementType).</summary>
    NotAModelElement,

    /// <summary>No parameter with the requested name exists on the element
    /// instance.</summary>
    ParameterNotFound,

    /// <summary>A parameter with that name exists, but only on the element's
    /// type/symbol, not on the instance itself.</summary>
    TypeParameterOnly,

    /// <summary>More than one parameter on the element matches the requested
    /// name (e.g. a shared parameter and a project parameter with the same
    /// display name). Element.LookupParameter would pick one arbitrarily, so
    /// this is reported instead of guessed.</summary>
    AmbiguousParameterName,

    /// <summary>The matched parameter's StorageType is not String.</summary>
    NotATextParameter,

    /// <summary>The matched parameter exists but is read-only
    /// (Parameter.IsReadOnly).</summary>
    ReadOnlyParameter,

    /// <summary>Parameter.Set(string) returned false or threw, even though
    /// every precondition above was satisfied.</summary>
    SetRejectedByRevit,

    /// <summary>The element could not be written to because of worksharing
    /// ownership (checked out to another user or another workset).</summary>
    ElementNotOwned
}
