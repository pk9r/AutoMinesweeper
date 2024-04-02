using System.Threading.Tasks;

using AutoMinesweeper.Abstractions;

using ImageSearch2020.Abstractions;
using ImageSearch2020.Payloads;

namespace AutoMinesweeper.Infrastructure;
internal class ImageSearch2020Service : IAsyncPixelFromWindowService<string>
{
    private readonly IImageSearch2020 _imageSearch2020;

    public ImageSearch2020Service(IImageSearch2020 imageSearch2020)
    {
        _imageSearch2020 = imageSearch2020;
    }

    public async Task<int> GetPixelFromWindowAsync(string title, int x, int y)
    {
        var reponse = await _imageSearch2020
            .GetPixelFromWindowAsync(new(
                SType: SType.Title,
                SValue: title,
                x, y));

        return reponse.ToInt();
    }
}
