using System.Threading;
using System.Threading.Tasks;

namespace NuGetFeed.Authentication;

public interface IAuthenticationService
{
    Task<bool> AuthenticateAsync(string apiKey, CancellationToken cancellationToken);
}
