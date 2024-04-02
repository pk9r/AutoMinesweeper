using System;
using System.Collections.Generic;
using System.Runtime.Versioning;
using System.Threading.Tasks;
using System.Windows;

using AutoMinesweeper.Models;

using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

using Windows.Win32;
using Windows.Win32.Foundation;

namespace AutoMinesweeper.ViewModels;

public partial class AutoMinesweeperViewModel : ObservableObject
{
    #region Constants
    private const int CELL_SIZE = 16;
    private const int BASE_CELL_X = 12;
    private const int BASE_CELL_Y = 55;
    private const int OFFSET_WIDTH = 26;
    private const int OFFSET_HEIGHT = 112;
    private const int OFFSET_GAME_LOSE_X = 5;
    private const int GAME_LOSE_Y = 32;
    private const int OFFSET_GAMEWIN_X = 8;
    private const int GAME_WIN_Y = 28;
    private const int OFFSET_COLOR_CELL_X = 9;
    private const int OFFSET_COLOR_CELL_Y = 12;
    private const string TITLE_GAME = "Minesweeper";
    private const string CLASS_GAME = "Minesweeper";
    private const int CELL_NULL = -2;
    private const int CELL_FLAG = -1;
    private const int COLOR_0 = 0xC0C0C0;
    private const int COLOR_1 = 0x0000FF;
    private const int COLOR_2 = 0x008000;
    private const int COLOR_3 = 0xFF0000;
    private const int COLOR_4 = 0x000080;
    private const int COLOR_5 = 0x800000;
    private const int COLOR_6 = 0x008080;
    private const int COLOR_7 = 0x000000;
    private const int K_F2 = 113;
    #endregion

    [ObservableProperty]
    private bool _isAutoResetGame;

    [ObservableProperty]
    private bool _isOpenRandomCell;

    [ObservableProperty]
    private bool _hasImageSearch2020;

    [ObservableProperty]
    private int _timeDelayOpenCell = 100;

    private int[,] _mineMatrix;
    private readonly List<CellPosition> _availableCells = [];

    private bool _hasHwndGame;
    private HWND _gameHwnd = HWND.Null;

    private int _numRow, _numColumn;
    private int _gameLoseX, _gameLoseY;
    private int _gameWinX, _gameWinY;

    public AutoMinesweeperViewModel()
    {
        IsAutoResetGame = false;
        IsOpenRandomCell = false;
        HasImageSearch2020 = false;
    }

    [SupportedOSPlatform("windows5.0")]
    [RelayCommand]
    internal async Task AutoPlayGameAsync()
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

        UpdateGameLayout();

        InitializeMineMatrix();
        await NewGameAsync();

        await StartRandomCellOpeningAsync();

        bool isGameWin;

        do
        {
            await SolveLevel1Async();

            await Task.Delay(500);

            var solveLevel2 = SolveLevel2();
            if (!solveLevel2)
            {
                var solveOpenLevel2 = await SolveOpenLevel2Async();
                if (!solveOpenLevel2)
                {
                    await Task.Delay(3000);

                    isGameWin = await IsGameWinAsync();
                    if (isGameWin)
                    {
                        break;
                    }

                    if (IsOpenRandomCell)
                    {
                        await OpenRandomCellAsync();
                        var isGameLose = await IsGameLoseAsync();

                        if (isGameLose)
                        {
                            if (IsAutoResetGame)
                            {
                                await Task.Delay(3000);
                                await AutoPlayGameAsync();
                                return;
                            }
                            else
                            {
                                break;
                            }
                        }
                    }
                    else
                    {
                        break;
                    }
                }
            }

            isGameWin = await IsGameWinAsync();
        } while (!isGameWin);
    }

    [SupportedOSPlatform("windows5.0")]
    private async Task NewGameAsync()
    {
        Control.SendKeyDown(_gameHwnd, K_F2);
        await Task.Delay(200);
    }

    [SupportedOSPlatform("windows5.0")]
    private async Task SolveLevel1Async()
    {
        bool hasOpened;
        do
        {
            hasOpened = false;

            SetFlagLevel1();

            for (var rowIndex = 0; rowIndex < _numRow; rowIndex++)
            {
                for (var colIndex = 0; colIndex < _numColumn; colIndex++)
                {
                    var cellPos = CellPosition.GetCellPosition(rowIndex, colIndex);

                    if (CanOpenAround(cellPos))
                    {
                        await OpenCellAroundAsync(cellPos);
                        hasOpened = true;
                    }
                }
            }
        } while (hasOpened);
    }

    private bool SolveLevel2()
    {
        for (var rowIndex1 = 0; rowIndex1 < _numRow; rowIndex1++)
        {
            for (var colIndex1 = 0; colIndex1 < _numColumn; colIndex1++)
            {
                var cellPos1 = CellPosition.GetCellPosition(rowIndex1, colIndex1);

                if (!IsNullCellOrFlagCell(cellPos1))
                {
                    for (var rowIndex2 = rowIndex1 - 2; rowIndex2 <= rowIndex1 + 2; rowIndex2++)
                    {
                        for (var colIndex2 = colIndex1 - 2; colIndex2 <= colIndex1 + 2; colIndex2++)
                        {
                            var cellPos2 = CellPosition.GetCellPosition(rowIndex2, colIndex2);

                            if (IsValidCell(cellPos2) &&
                                !IsSameCell(cellPos1, cellPos2) &&
                                CanSolveLevel2(cellPos1, cellPos2))
                            {
                                SetFlagLevel2(cellPos1, cellPos2);
                                return true;
                            }
                        }
                    }
                }
            }
        }

        return false;
    }

    [SupportedOSPlatform("windows5.0")]
    private async ValueTask<bool> SolveOpenLevel2Async()
    {
        for (var rowIndex1 = 0; rowIndex1 < _numRow; rowIndex1++)
        {
            for (var colIndex1 = 0; colIndex1 < _numColumn; colIndex1++)
            {
                var cellPos1 = CellPosition.GetCellPosition(rowIndex1, colIndex1);

                if (!IsNullCellOrFlagCell(cellPos1))
                {
                    for (var rowIndex2 = rowIndex1 - 2; rowIndex2 <= rowIndex1 + 2; rowIndex2++)
                    {
                        for (var colIndex2 = colIndex1 - 2; colIndex2 <= colIndex1 + 2; colIndex2++)
                        {
                            var cellPos2 = CellPosition.GetCellPosition(rowIndex2, colIndex2);

                            if (IsValidCell(cellPos2) &&
                                !IsSameCell(cellPos1, cellPos2) &&
                                CanOpenLevel2(cellPos1, cellPos2))
                            {
                                await OpenCellLevel2Async(cellPos1, cellPos2);
                                return true;
                            }
                        }
                    }
                }
            }
        }
        return false;
    }

    [SupportedOSPlatform("windows5.0")]
    private async ValueTask<bool> IsGameLoseAsync() =>
    await GetPixelAsync(TITLE_GAME, _gameHwnd, _gameLoseX, _gameLoseY) == 0;

    [SupportedOSPlatform("windows5.0")]
    private async ValueTask<bool> IsGameWinAsync() =>
        await GetPixelAsync(TITLE_GAME, _gameHwnd, _gameWinX, _gameWinY) == 0;

    [SupportedOSPlatform("windows5.0")]
    private bool UpdateHwndGame()
    {
        _gameHwnd = PInvoke.FindWindow(CLASS_GAME, TITLE_GAME);
        return _gameHwnd != HWND.Null;
    }

    [SupportedOSPlatform("windows5.0")]
    private void UpdateGameLayout()
    {
        PInvoke.GetWindowRect(_gameHwnd, out var lpRect);
        _numRow = (lpRect.bottom - lpRect.top - OFFSET_HEIGHT) / CELL_SIZE;
        _numColumn = (lpRect.right - lpRect.left - OFFSET_WIDTH) / CELL_SIZE;
        _gameLoseX = ((lpRect.right - lpRect.left) / 2) - OFFSET_GAME_LOSE_X;
        _gameLoseY = GAME_LOSE_Y;
        _gameWinX = ((lpRect.right - lpRect.left) / 2) - OFFSET_GAMEWIN_X;
        _gameWinY = GAME_WIN_Y;
    }

    private void InitializeMineMatrix()
    {
        _mineMatrix = new int[_numRow, _numColumn];
        for (var i = 0; i < _numRow; i++)
        {
            for (var j = 0; j < _numColumn; j++)
            {
                _mineMatrix[i, j] = CELL_NULL;
            }
        }

        _availableCells.Clear();
        for (var rowIndex = 0; rowIndex < _numRow; rowIndex++)
        {
            for (var colIndex = 0; colIndex < _numColumn; colIndex++)
            {
                _availableCells.Add(CellPosition.GetCellPosition(rowIndex, colIndex));
            }
        }
    }

    [SupportedOSPlatform("windows5.0")]
    private async ValueTask<int> GetPixelAsync(string title, HWND hWnd, int x, int y)
    {
        if (HasImageSearch2020)
        {
            return await Control.GetPixelFromWindowAsync("title", title, x, y);
        }

        return Control.GetPixelFromWindow(hWnd, x, y);
    }

    [SupportedOSPlatform("windows5.0")]
    private async ValueTask<int> ObtainCellColorAsync(CellPosition cellPos)
    {
        var x = BASE_CELL_X + (cellPos.ColIndex * CELL_SIZE) + OFFSET_COLOR_CELL_X;
        var y = BASE_CELL_Y + (cellPos.RowIndex * CELL_SIZE) + OFFSET_COLOR_CELL_Y;
        var color = await GetPixelAsync(TITLE_GAME, _gameHwnd, x, y);
        int result;

        result = color switch
        {
            COLOR_0 => await RetrieveColorZeroValue(),
            COLOR_1 => 1,
            COLOR_2 => 2,
            COLOR_3 => 3,
            COLOR_4 => 4,
            COLOR_5 => 5,
            COLOR_6 => 6,
            COLOR_7 => 7,
            _ => 8
        };

        return result;

        async ValueTask<int> RetrieveColorZeroValue()
        {
            var x = BASE_CELL_X + (cellPos.ColIndex * CELL_SIZE) + 1;
            var y = BASE_CELL_Y + (cellPos.RowIndex * CELL_SIZE) + 1;
            var color = await GetPixelAsync(TITLE_GAME, _gameHwnd, x, y);
            if (color != COLOR_0)
            {
                return CELL_NULL;
            }

            return 0;
        }
    }

    [SupportedOSPlatform("windows5.0")]
    private async ValueTask UpdateCellValueAsync(CellPosition cellPos)
    {
        if (GetCellValue(cellPos) == CELL_NULL)
        {
            var cellValue = await ObtainCellColorAsync(cellPos);
            SetValue(cellPos, cellValue);
            if (cellValue != CELL_NULL)
            {
                _availableCells.Remove(cellPos);
                if (GetCellValue(cellPos) == 0)
                {
                    await UpdateCellValuesAroundAsync(cellPos);
                }
                return;
            }
        }
    }

    [SupportedOSPlatform("windows5.0")]
    private async Task UpdateCellValuesAroundAsync(CellPosition centerCellPos)
    {
        for (var rowIndex = centerCellPos.RowIndex - 1; rowIndex <= centerCellPos.RowIndex + 1; rowIndex++)
        {
            for (var colIndex = centerCellPos.ColIndex - 1; colIndex <= centerCellPos.ColIndex + 1; colIndex++)
            {
                var cellPos = CellPosition.GetCellPosition(rowIndex, colIndex);

                if (IsValidCell(cellPos) &&
                    !IsSameCell(cellPos, centerCellPos))
                {
                    await UpdateCellValueAsync(cellPos);
                }
            }
        }
    }

    [SupportedOSPlatform("windows5.0")]
    private async Task OpenRandomCellAsync()
    {
        bool isOpened;
        int rIndex;
        CellPosition rCellPosition;

        do
        {
            rIndex = Random.Shared.Next(0, _availableCells.Count - 1);
            rCellPosition = _availableCells[rIndex];
            if (isOpened = GetCellValue(rCellPosition) != CELL_FLAG)
            {
                await OpenCellAsync(rCellPosition);
            }
        } while (!isOpened);
    }

    [SupportedOSPlatform("windows5.0")]
    private async Task StartRandomCellOpeningAsync()
    {
        while (!ShouldPauseOpenRandomCell())
        {
            await OpenRandomCellAsync();
            if (await IsGameLoseAsync())
            {
                InitializeMineMatrix();
                await NewGameAsync();
            }
        }
    }

    [SupportedOSPlatform("windows5.0")]
    private async Task OpenCellAsync(CellPosition cellPos)
    {
        var x = BASE_CELL_X + (cellPos.ColIndex * CELL_SIZE) + (CELL_SIZE / 2);
        var y = BASE_CELL_Y + (cellPos.RowIndex * CELL_SIZE) + (CELL_SIZE / 2);
        Control.ControlClick(_gameHwnd, x, y);

        await Task.Delay(TimeDelayOpenCell);

        await UpdateCellValueAsync(cellPos);
    }

    [SupportedOSPlatform("windows5.0")]
    private async Task OpenCellAroundAsync(CellPosition centerCellPos)
    {
        for (var rowIndex = centerCellPos.RowIndex - 1; rowIndex <= centerCellPos.RowIndex + 1; rowIndex++)
        {
            for (var colIndex = centerCellPos.ColIndex - 1; colIndex <= centerCellPos.ColIndex + 1; colIndex++)
            {
                var cellPos = CellPosition.GetCellPosition(rowIndex, colIndex);

                if (IsValidCell(cellPos) &&
                    !IsSameCell(cellPos, centerCellPos) &&
                    GetCellValue(cellPos) == CELL_NULL)
                {
                    await OpenCellAsync(cellPos);
                }
            }
        }
    }

    [SupportedOSPlatform("windows5.0")]
    private async Task OpenCellLevel2Async(CellPosition cellPos1, CellPosition cellPos2)
    {
        for (var rowIndex = cellPos1.RowIndex - 1; rowIndex <= cellPos1.RowIndex + 1; rowIndex++)
        {
            for (var colIndex = cellPos1.ColIndex - 1; colIndex <= cellPos1.ColIndex + 1; colIndex++)
            {
                var cellPos = CellPosition.GetCellPosition(rowIndex, colIndex);

                if (IsValidCell(cellPos) &&
                    GetCellValue(cellPos) == CELL_NULL &&
                    !IsAdjacentCells(cellPos, cellPos2))
                {
                    await OpenCellAsync(cellPos);
                }
            }
        }
    }

    private void SetFlagAround(CellPosition centerCellPos)
    {
        for (var rowIndex = centerCellPos.RowIndex - 1; rowIndex <= centerCellPos.RowIndex + 1; rowIndex++)
        {
            for (var colIndex = centerCellPos.ColIndex - 1; colIndex <= centerCellPos.ColIndex + 1; colIndex++)
            {
                var cellPos = CellPosition.GetCellPosition(rowIndex, colIndex);

                if (IsValidCell(cellPos) &&
                    GetCellValue(cellPos) == CELL_NULL)
                {
                    SetFlag(cellPos);
                }
            }
        }
    }

    private void SetFlagLevel1()
    {
        int nUnopenedCellsAround;

        for (var rowIndex = 0; rowIndex < _numRow; rowIndex++)
        {
            for (var colIndex = 0; colIndex < _numColumn; colIndex++)
            {
                var cellPos = CellPosition.GetCellPosition(rowIndex, colIndex);

                if (!IsNullCellOrFlagCell(cellPos, out var cellValue))
                {
                    nUnopenedCellsAround = CountUnopenedCellsAround(cellPos);

                    if (cellValue == nUnopenedCellsAround &&
                        cellValue > CountCellFlagsAround(cellPos))
                    {
                        SetFlagAround(cellPos);
                    }
                }
            }
        }
    }

    private void SetFlagLevel2(CellPosition cellPos1, CellPosition cellPos2)
    {
        for (var rowIndex = cellPos1.RowIndex - 1; rowIndex <= cellPos1.RowIndex + 1; rowIndex++)
        {
            for (var colIndex = cellPos1.ColIndex - 1; colIndex <= cellPos1.ColIndex + 1; colIndex++)
            {
                var cellPos = CellPosition.GetCellPosition(rowIndex, colIndex);

                if (IsValidCell(cellPos) &&
                    GetCellValue(cellPos) == CELL_NULL &&
                    !IsAdjacentCells(cellPos, cellPos2))
                {
                    SetFlag(cellPos);
                }
            }
        }
    }

    private bool ShouldPauseOpenRandomCell()
    {
        for (var rowIndex = 0; rowIndex < _numRow; rowIndex++)
        {
            for (var colIndex = 0; colIndex < _numColumn; colIndex++)
            {
                if (_mineMatrix[rowIndex, colIndex] == 0)
                {
                    return true;
                }
            }
        }

        return false;
    }

    private bool CanOpenAround(CellPosition cellPos)
    {
        return
            IsNullCellOrFlagCell(cellPos, out var cellValue) &&
            CountNullCellsAround(cellPos) > 0 &&
            CountCellFlagsAround(cellPos) == cellValue;
    }

    private bool CanOpenLevel2(CellPosition cellPos1, CellPosition cellPos2)
    {
        if (IsNullCellOrFlagCell(cellPos2))
        {
            return false;
        }
        // cellPos1 checked before

        var nullCellsAround1Count = CountNullCellsAround(cellPos1);
        var nullCellsAround2Count = CountNullCellsAround(cellPos2);

        var unmarkedFlagCount1 = GetCellValue(cellPos1) - CountCellFlagsAround(cellPos1);
        var unmarkedFlagCount2 = GetCellValue(cellPos2) - CountCellFlagsAround(cellPos2);

        var nullCellsInAdjacentAreaCount = CountNullCellsInAdjacentArea(cellPos1, cellPos2);

        var nullCellsOpenCount = nullCellsAround1Count - nullCellsInAdjacentAreaCount;

        var hasCellOpenLevel2 = nullCellsOpenCount > 0;
        //var isValidOpenLevel2 = unmarkedFlagCount1 == unmarkedFlagCount2 - nullCellsAround2Count + nullCellsInAdjacentAreaCount;
        var isValidOpenLevel2 = nullCellsAround2Count - nullCellsInAdjacentAreaCount == unmarkedFlagCount2 - unmarkedFlagCount1;

        return hasCellOpenLevel2 && isValidOpenLevel2;
    }

    private bool CanSolveLevel2(CellPosition cellPos1, CellPosition cellPos2)
    {
        if (IsNullCellOrFlagCell(cellPos2))
        {
            return false;
        }
        // cellPos1 checked before

        var nullCellsAround1Count = CountNullCellsAround(cellPos1);

        var unmarkedFlagCount1 = GetCellValue(cellPos1) - CountCellFlagsAround(cellPos1);
        var unmarkedFlagCount2 = GetCellValue(cellPos2) - CountCellFlagsAround(cellPos2);

        var nullCellsInAdjacentAreaCount = CountNullCellsInAdjacentArea(cellPos1, cellPos2);

        var nullCellsFlagCount = nullCellsAround1Count - nullCellsInAdjacentAreaCount;

        var hasCellOpenLevel2 = nullCellsFlagCount > 0;

        // Suppose, all mines of "cell2" are in nullCellsInAdjacentArea.
        // If the remaining number of "cell1" is the number of "remaining nullCellsAround1"
        // Then the hypothesis is correct
        var remainingMines = unmarkedFlagCount1 - unmarkedFlagCount2;
        var remainingNullCellsAround = nullCellsAround1Count - nullCellsInAdjacentAreaCount;
        var hypo = remainingMines == remainingNullCellsAround;

        return hasCellOpenLevel2 && hypo;
    }

    private int CountUnopenedCellsAround(CellPosition centerCellPos)
    {
        var nCount = 0;
        for (var rowIndex = centerCellPos.RowIndex - 1; rowIndex <= centerCellPos.RowIndex + 1; rowIndex++)
        {
            for (var colIndex = centerCellPos.ColIndex - 1; colIndex <= centerCellPos.ColIndex + 1; colIndex++)
            {
                var cellPos = CellPosition.GetCellPosition(rowIndex, colIndex);

                if (IsValidCell(cellPos) &&
                    IsNullCellOrFlagCell(cellPos) &&
                    !IsSameCell(cellPos, centerCellPos))
                {
                    nCount++;
                }
            }
        }

        return nCount;
    }

    private int CountNullCellsAround(CellPosition centerCellPos)
    {
        var nCount = 0;
        for (var rowIndex = centerCellPos.RowIndex - 1; rowIndex <= centerCellPos.RowIndex + 1; rowIndex++)
        {
            for (var colIndex = centerCellPos.ColIndex - 1; colIndex <= centerCellPos.ColIndex + 1; colIndex++)
            {
                var cellPos = CellPosition.GetCellPosition(rowIndex, colIndex);

                if (IsValidCell(cellPos) &&
                    GetCellValue(cellPos) == CELL_NULL &&
                    !IsSameCell(cellPos, centerCellPos))
                {
                    nCount++;
                }
            }
        }

        return nCount;
    }

    private int CountCellFlagsAround(CellPosition centerCellPos)
    {
        var nCount = 0;
        for (var rowIndex = centerCellPos.RowIndex - 1; rowIndex <= centerCellPos.RowIndex + 1; rowIndex++)
        {
            for (var colIndex = centerCellPos.ColIndex - 1; colIndex <= centerCellPos.ColIndex + 1; colIndex++)
            {
                var cellPos = CellPosition.GetCellPosition(rowIndex, colIndex);

                if (IsValidCell(cellPos) &&
                    GetCellValue(cellPos) == CELL_FLAG &&
                    !IsSameCell(cellPos, centerCellPos))
                {
                    nCount++;
                }
            }
        }

        return nCount;
    }

    private int CountNullCellsInAdjacentArea(CellPosition celPos1, CellPosition celPos2)
    {
        var nCount = 0;

        for (var rowIndex = celPos1.RowIndex - 1; rowIndex <= celPos1.RowIndex + 1; rowIndex++)
        {
            for (var colIndex = celPos1.ColIndex - 1; colIndex <= celPos1.ColIndex + 1; colIndex++)
            {
                var cellPos = CellPosition.GetCellPosition(rowIndex, colIndex);

                if (IsValidCell(cellPos) &&
                    IsAdjacentCells(cellPos, celPos2) &&
                    GetCellValue(cellPos) == CELL_NULL)
                {
                    nCount++;
                }
            }
        }

        return nCount;
    }

    private static bool IsAdjacentCells(CellPosition cellPos1, CellPosition cellPos2)
    {
        return Math.Abs(cellPos1.RowIndex - cellPos2.RowIndex) <= 1 &&
            Math.Abs(cellPos1.ColIndex - cellPos2.ColIndex) <= 1;
    }

    private int GetCellValue(CellPosition cellPos)
    {
        return _mineMatrix[cellPos.RowIndex, cellPos.ColIndex];
    }

    private bool IsValidCell(CellPosition cellPos)
    {
        return cellPos.RowIndex >= 0 &&
            cellPos.RowIndex < _numRow &&
            cellPos.ColIndex >= 0 &&
            cellPos.ColIndex < _numColumn;
    }

    private bool IsNullCellOrFlagCell(CellPosition cellPos)
    {
        return GetCellValue(cellPos) <= 0;
    }

    private bool IsNullCellOrFlagCell(CellPosition cellPos, out int cellValue)
    {
        cellValue = GetCellValue(cellPos);
        return cellValue <= 0;
    }

    private static bool IsSameCell(CellPosition cellPos1, CellPosition cellPos2)
    {
        return cellPos1.RowIndex == cellPos2.RowIndex &&
            cellPos1.ColIndex == cellPos2.ColIndex;
    }

    private void SetValue(CellPosition cellPos, int value)
    {
        _mineMatrix[cellPos.RowIndex, cellPos.ColIndex] = value;
    }

    private void SetFlag(CellPosition cellPos)
    {
        SetValue(cellPos, CELL_FLAG);
    }
}
