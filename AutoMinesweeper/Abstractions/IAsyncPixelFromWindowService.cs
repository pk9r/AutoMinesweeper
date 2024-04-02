using System.Threading.Tasks;

namespace AutoMinesweeper.Abstractions;
public interface IAsyncPixelFromWindowService<IdentityWindowT>
{
    Task<int> GetPixelFromWindowAsync(IdentityWindowT identityWindow, int x, int y);
}
