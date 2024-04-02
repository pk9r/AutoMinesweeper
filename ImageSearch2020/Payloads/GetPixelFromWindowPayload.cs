namespace ImageSearch2020.Payloads;

/// <summary>
/// Get Pixel From Window
/// </summary>
/// <param name="SType"></param>
/// <param name="SValue"></param>
/// <param name="X"></param>
/// <param name="Y"></param>
/// <param name="PW">Hỗ trợ của sổ nhúng Chrome</param>
public record GetPixelFromWindowPayload(
    SType SType,
    string SValue,
    int X,
    int Y,
    bool PW = false)
{
    internal HttpContent ToContent()
    {
        return new StringContent(
            $$"""
            {
                "SType": "{{SType.Value}}",
                "SValue": "{{SValue}}",
                "X": {{X}},
                "Y": {{Y}},
                "PW": {{PW.ToString().ToLowerInvariant()}}
            }
            """);
    }
}
