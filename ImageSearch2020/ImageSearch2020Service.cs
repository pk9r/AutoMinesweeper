using System.Net.Http.Json;

using ImageSearch2020.Abstractions;
using ImageSearch2020.Payloads;
using ImageSearch2020.Responses;

namespace ImageSearch2020;
public class ImageSearch2020Service : IImageSearch2020
{
    private readonly HttpClient _httpClient;

    public ImageSearch2020Service(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public async Task<ColorReponse?> GetPixelFromWindowAsync(GetPixelFromWindowPayload payload)
    {
        var response = await _httpClient.PostAsync(
            "http://localhost:2020/getPixelFromWindow", payload.ToContent());
        response.EnsureSuccessStatusCode();

        var colorResponse = await response.Content.ReadFromJsonAsync<ColorReponse>();
        return colorResponse;
    }
}
