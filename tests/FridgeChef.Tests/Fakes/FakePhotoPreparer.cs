using FridgeChef.Services.Api;
using Microsoft.AspNetCore.Components.Forms;

namespace FridgeChef.Tests.Fakes;

public sealed class FakePhotoPreparer : IPhotoPreparer
{
    public Exception? Throw { get; set; }

    public async Task<PreparedPhoto> PrepareAsync(IBrowserFile file, CancellationToken cancellationToken = default)
    {
        if (Throw is not null)
        {
            throw Throw;
        }

        using var buffer = new MemoryStream();
        await file.OpenReadStream(cancellationToken: cancellationToken).CopyToAsync(buffer, cancellationToken);
        return new PreparedPhoto(buffer.ToArray(), "image/jpeg");
    }
}
