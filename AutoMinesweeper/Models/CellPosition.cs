namespace AutoMinesweeper.Models;
public record CellPosition(int RowIndex, int ColIndex)
{
    public static CellPosition GetCellPosition(int rowIndex, int colIndex) =>
        new(rowIndex, colIndex);
}
