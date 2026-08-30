using Autodesk.Revit.DB;
using BatchParameterUpdate.Core;

namespace BatchParameterUpdate.Revit.Resolution;

/// <summary>
/// Classifies a single element against a <see cref="ParameterUpdateRequest"/>
/// and, when possible, writes the new value. This is the only class that
/// touches Autodesk.Revit.DB.Parameter/Element directly; every other type
/// involved (SkipReason, ElementOutcome) lives in Core.
///
/// This class is intentionally NOT unit-tested: Element and Parameter cannot
/// be constructed or mocked outside a live Revit document (see README >
/// Design decisions), so its correctness is verified manually against a real
/// Revit session instead.
/// </summary>
public static class ParameterResolver
{
    /// <summary>
    /// Resolves and, if valid, applies the update to a single element.
    /// Must be called inside an open Transaction.
    /// </summary>
    public static ElementOutcome Resolve(Element element, ParameterUpdateRequest request)
    {
        var elementId = element.Id.Value;
        var description = Describe(element);

        // Case A: the "element" is actually a type/symbol (e.g. a wall type
        // picked up because the selection filter was too broad). Types do not
        // carry the instance parameters this command targets.
        if (element is ElementType)
            return ElementOutcome.Skipped(elementId, description, SkipReason.NotAModelElement,
                "Element is a type, not a model instance.");

        // GetParameters (not LookupParameter) on purpose: LookupParameter
        // returns the first match when several parameters share a display
        // name, and Autodesk's own documentation notes that match is
        // effectively arbitrary. Comparing case-sensitively via
        // Definition.Name mirrors how Revit itself stores parameter names.
        var matches = element.GetParameters(request.TrimmedParameterName);

        if (matches.Count == 0)
        {
            // Distinguish "does not exist at all" from "exists on the type
            // only" — the latter is a much more actionable message for the
            // user than a flat "not found".
            if (ExistsOnType(element, request.TrimmedParameterName))
                return ElementOutcome.Skipped(elementId, description, SkipReason.TypeParameterOnly,
                    $"Parameter '{request.TrimmedParameterName}' exists on the type, not on this instance.");

            return ElementOutcome.Skipped(elementId, description, SkipReason.ParameterNotFound,
                $"Parameter '{request.TrimmedParameterName}' was not found on this element.");
        }

        if (matches.Count > 1)
            return ElementOutcome.Skipped(elementId, description, SkipReason.AmbiguousParameterName,
                $"{matches.Count} parameters named '{request.TrimmedParameterName}' were found; skipped rather than guessing which one to write.");

        var parameter = matches[0];

        if (parameter.StorageType != StorageType.String)
            return ElementOutcome.Skipped(elementId, description, SkipReason.NotATextParameter,
                $"Parameter '{request.TrimmedParameterName}' is not a text parameter (StorageType: {parameter.StorageType}).");

        if (parameter.IsReadOnly)
            return ElementOutcome.Skipped(elementId, description, SkipReason.ReadOnlyParameter,
                $"Parameter '{request.TrimmedParameterName}' is read-only on this element.");

        var currentValue = parameter.AsString() ?? string.Empty;
        if (string.Equals(currentValue, request.NewValue, StringComparison.Ordinal))
            return ElementOutcome.Unchanged(elementId, description,
                $"Value already equals '{request.NewValue}'.");

        bool accepted;
        try
        {
            accepted = parameter.Set(request.NewValue);
        }
        catch (Autodesk.Revit.Exceptions.ApplicationException)
        {
            // Covers worksharing rejections (element checked out elsewhere)
            // and other host-level refusals that surface as an exception
            // rather than a `false` return.
            return ElementOutcome.Skipped(elementId, description, SkipReason.ElementNotOwned,
                "The element could not be modified (ownership/worksharing).");
        }

        if (!accepted)
            return ElementOutcome.Skipped(elementId, description, SkipReason.SetRejectedByRevit,
                $"Revit rejected the write of '{request.NewValue}'.");

        return ElementOutcome.Updated(elementId, description,
            $"Set to '{request.NewValue}' (was '{currentValue}').");
    }

    private static bool ExistsOnType(Element element, string parameterName)
    {
        if (element.Document.GetElement(element.GetTypeId()) is not { } type)
            return false;

        return type.GetParameters(parameterName).Count > 0;
    }

    private static string Describe(Element element) =>
        $"{element.Category?.Name ?? "Unknown category"} (Id {element.Id.Value})";
}
