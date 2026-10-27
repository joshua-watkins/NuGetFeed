using System;
using System.Collections.Generic;
using System.Security.Cryptography.X509Certificates;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using NuGetFeed.Models;
using NuGetFeed.Signing;

namespace NuGetFeed.Controllers;

/// <summary>
/// The Repository Signatures resource, which tells clients which certificates sign this feed's packages.
/// See: https://learn.microsoft.com/en-us/nuget/api/repository-signatures-resource
/// </summary>
public class RepositorySignaturesController : Controller
{
    private readonly IRepositorySigningService _signing;
    private readonly IPackageDatabase _packages;
    private readonly IUrlGenerator _url;

    public RepositorySignaturesController(IRepositorySigningService signing, IPackageDatabase packages, IUrlGenerator url)
    {
        _signing = signing ?? throw new ArgumentNullException(nameof(signing));
        _packages = packages ?? throw new ArgumentNullException(nameof(packages));
        _url = url ?? throw new ArgumentNullException(nameof(url));
    }

    // GET v3/repository-signatures/{resourceVersion}/index.json
    [HttpGet]
    public async Task<ActionResult<RepositorySignaturesResponse>> IndexAsync(string resourceVersion, CancellationToken cancellationToken)
    {
        if (_signing.Certificate == null) return NotFound();

        bool allRepositorySigned;
        switch (resourceVersion)
        {
            // NuGet 4.7 to 4.9 clients can't install packages from sources that claim
            // all packages are signed, so the older resource must always say false.
            case "4.7.0":
                allRepositorySigned = false;
                break;

            case "5.0.0":
                allRepositorySigned = await _packages.AllRepositorySignedAsync(cancellationToken);
                break;

            default:
                return NotFound();
        }

        var certificate = _signing.Certificate;

        return new RepositorySignaturesResponse
        {
            AllRepositorySigned = allRepositorySigned,
            SigningCertificates = new[]
            {
                new RepositorySigningCertificate
                {
                    ContentUrl = _url.GetRepositorySigningCertificateUrl(_signing.CertificateFingerprint),
                    Fingerprints = new Dictionary<string, string>
                    {
                        { RepositorySigningCertificate.Sha256Oid, _signing.CertificateFingerprint },
                    },
                    Subject = certificate.Subject,
                    Issuer = certificate.Issuer,
                    NotBefore = certificate.NotBefore.ToUniversalTime(),
                    NotAfter = certificate.NotAfter.ToUniversalTime(),
                }
            },
        };
    }

    // GET v3/repository-signatures/certificates/{fingerprint}.crt
    [HttpGet]
    public IActionResult Certificate(string fingerprint)
    {
        if (_signing.Certificate == null ||
            !string.Equals(fingerprint, _signing.CertificateFingerprint, StringComparison.OrdinalIgnoreCase))
        {
            return NotFound();
        }

        return File(_signing.Certificate.Export(X509ContentType.Cert), "application/pkix-cert");
    }
}
