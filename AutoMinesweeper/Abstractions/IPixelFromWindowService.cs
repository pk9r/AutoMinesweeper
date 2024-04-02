
namespace AutoMinesweeper.Abstractions;

internal interface IPixelFromWindowService<IdentityWindowT>
{
    int GetPixelFromWindow(IdentityWindowT identityWindow, int x, int y);
}
