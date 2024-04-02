using AutoMinesweeper.Models;
using AutoMinesweeper.Services;

namespace AutoMinesweeper.WinmineXP;
public record GameLayout(
    int NumRow,
    int NumCol,
    PixelPosition GameWinPos,
    PixelPosition GameLosePos) : IGameLayout
{
}
