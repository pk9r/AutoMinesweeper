using ImageSearch2020.Payloads;
using ImageSearch2020.Responses;

namespace ImageSearch2020.Abstractions;
public interface IImageSearch2020
{
    Task<ColorReponse?> GetPixelFromWindowAsync(GetPixelFromWindowPayload payload);
}
