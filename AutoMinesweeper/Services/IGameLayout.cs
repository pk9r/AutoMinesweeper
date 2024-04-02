namespace AutoMinesweeper.Services;

public interface IGameLayout
{
    public int NumRow { get; }
    public int NumCol { get; }

    public (int numRow, int numCol) GetDimension()
    {
        return (NumRow, NumCol);
    }
}
