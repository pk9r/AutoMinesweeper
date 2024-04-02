using System.Runtime.Versioning;
using System.Threading.Tasks;
using System.Windows;

using AutoMinesweeper.Infrastructure;
using AutoMinesweeper.Models;

using Windows.Win32;
using Windows.Win32.Foundation;

namespace AutoMinesweeper.WinmineXP;
internal class GameInteractor
{
    #region Constants Region
    private const int CellSize = 16;
    private const int BaseCellX = 12;
    private const int BaseCellY = 55;
    private const int OffsetWidth = 26;
    private const int OffsetHeight = 112;
    private const int OffsetGameLoseX = 5;
    private const int GameLoseY = 32;
    private const int OffsetGameWinX = 8;
    private const int GameWinY = 28;
    private const int OffsetColorCellX = 9;
    private const int OffsetColorCellY = 12;
    private const string TitleGame = "Minesweeper";
    private const string ClassGame = "Minesweeper";
    private const int ColorCellZeroOrUnrevealedCell = 0xC0C0C0;
    private const int ColorCellOne = 0x0000FF;
    private const int ColorCellTwo = 0x008000;
    private const int ColorCellThree = 0xFF0000;
    private const int ColorCellFour = 0x000080;
    private const int ColorCellFive = 0x800000;
    private const int ColorCellSix = 0x008080;
    private const int ColorCellSeven = 0x000000;
    private const int ColorLose = 0x000000;
    private const int ColorWin = 0x000000;

    private const int KeycodeF2 = 113;
    #endregion

    private GameLayout _gameLayout;

    private bool _hasHwndGame;
    private HWND _gameHwnd = HWND.Null;

    private readonly NativeMethodsService _nativeMethodsService;

    public GameInteractor(NativeMethodsService nativeMethodsService)
    {
        _nativeMethodsService = nativeMethodsService;
    }

    [SupportedOSPlatform("windows5.0")]
    public async ValueTask<bool> IsGameLoseAsync()
    {
        var color = await GetPixelAsync(
            TitleGame,
            _gameHwnd,
            _gameLayout.GameLosePos.X,
            _gameLayout.GameLosePos.Y);
        return color == ColorLose;
    }

    [SupportedOSPlatform("windows5.0")]
    public async ValueTask<bool> IsGameWinAsync()
    {
        var color = await GetPixelAsync(
            TitleGame,
            _gameHwnd,
            _gameLayout.GameWinPos.X,
            _gameLayout.GameWinPos.Y);
        return color == ColorWin;
    }

    [SupportedOSPlatform("windows5.0")]
    public async ValueTask<CellValue> ObtainCellColorAsync(CellPosition cellPos)
    {
        var x = BaseCellX + (cellPos.ColIndex * CellSize) + OffsetColorCellX;
        var y = BaseCellY + (cellPos.RowIndex * CellSize) + OffsetColorCellY;

        var color = await GetPixelAsync(TitleGame, _gameHwnd, x, y);

        var cellValue = color switch
        {
            ColorCellZeroOrUnrevealedCell => await RetrieveColorZeroValue(),
            ColorCellOne => CellValue.One,
            ColorCellTwo => CellValue.Two,
            ColorCellThree => CellValue.Three,
            ColorCellFour => CellValue.Four,
            ColorCellFive => CellValue.Five,
            ColorCellSix => CellValue.Six,
            ColorCellSeven => CellValue.Seven,
            _ => CellValue.Eight
        };

        return cellValue;

        async ValueTask<CellValue> RetrieveColorZeroValue()
        {
            var x = BaseCellX + (cellPos.ColIndex * CellSize) + 1;
            var y = BaseCellY + (cellPos.RowIndex * CellSize) + 1;

            var color = await GetPixelAsync(TitleGame, _gameHwnd, x, y);

            return color == ColorCellZeroOrUnrevealedCell ?
                CellValue.Zero : CellValue.Unrevealed;
        }
    }

    [SupportedOSPlatform("windows5.0")]
    private bool UpdateHwndGame()
    {
        _gameHwnd = PInvoke.FindWindow(ClassGame, TitleGame);

        return _gameHwnd != HWND.Null;
    }

    [SupportedOSPlatform("windows5.0")]
    public bool UpdateGameLayout()
    {
        if (!_hasHwndGame || !PInvoke.IsWindowVisible(_gameHwnd))
        {
            while (!UpdateHwndGame())
            {
                var result = MessageBox.Show(
                    "Không tìm thấy cửa sổ game. Thử lại?",
                    "Lỗi",
                    button: MessageBoxButton.OKCancel);
                if (result == MessageBoxResult.Cancel)
                {
                    return;
                }
            }

            _hasHwndGame = true;
        }

        PInvoke.GetWindowRect(_gameHwnd, out var lpRect);

        var numRow = (lpRect.bottom - lpRect.top - OffsetHeight) / CellSize;
        var numCol = (lpRect.right - lpRect.left - OffsetWidth) / CellSize;
        var gameLoseX = ((lpRect.right - lpRect.left) / 2) - OffsetGameLoseX;
        var gameLoseY = GameLoseY;
        var gameWinX = ((lpRect.right - lpRect.left) / 2) - OffsetGameWinX;
        var gameWinY = GameWinY;

        _gameLayout = new(
            numRow,
            numCol,
            GameWinPos: new(gameWinX, gameWinY),
            GameLosePos: new(gameLoseX, gameLoseY));

        return true;
    }

    [SupportedOSPlatform("windows5.0")]
    public async Task OpenCellAsync(CellPosition cellPos)
    {
        var x = BaseCellX + (cellPos.ColIndex * CellSize) + (CellSize / 2);
        var y = BaseCellY + (cellPos.RowIndex * CellSize) + (CellSize / 2);
        _nativeMethodsService.ClickToWindow(_gameHwnd, x, y);

        await Task.Delay(TimeDelayOpenCell);

        await UpdateCellValueAsync(cellPos);
    }

    [SupportedOSPlatform("windows5.0")]
    public async Task NewGameAsync()
    {
        _nativeMethodsService.SendKeyWindow(_gameHwnd, KeycodeF2);
        await Task.Delay(200);
    }

    [SupportedOSPlatform("windows5.0")]
    private async ValueTask<int> GetPixelAsync(string title, HWND hWnd, int x, int y)
    {
        if (_useImageSearch2020)
        {
            return await Control.GetPixelFromWindowAsync("title", title, x, y);
        }

        return Control.GetPixelFromWindow(hWnd, x, y);
    }
}
