namespace AutoMinesweeper.Abstractions;
public interface IControlWindowService<IdentityWindowT>
{
    void ClickToWindow(IdentityWindowT identityWindow, int x, int y);

    void SendKeyToWindow(IdentityWindowT identityWindow, uint key);
}
