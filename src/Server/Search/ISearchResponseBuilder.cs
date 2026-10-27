using System.Collections.Generic;
using NuGetFeed.Metadata;
using NuGetFeed.Models;

namespace NuGetFeed.Search;

public interface ISearchResponseBuilder
{
    SearchResponse BuildSearch(IReadOnlyList<PackageRegistration> results);
    AutocompleteResponse BuildAutocomplete(IReadOnlyList<string> data);
    DependentsResponse BuildDependents(IReadOnlyList<PackageDependent> results);
}
