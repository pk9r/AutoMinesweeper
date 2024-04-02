namespace ImageSearch2020.Responses;
public record ColorReponse(string Color)
{
    public int ToInt() =>
        int.Parse(Color, System.Globalization.NumberStyles.HexNumber);
}
