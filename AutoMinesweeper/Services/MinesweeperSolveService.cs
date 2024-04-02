using System;
using System.Collections.Generic;
using System.Runtime.Versioning;
using System.Threading.Tasks;

using AutoMinesweeper.Models;
using AutoMinesweeper.WinmineXP;

namespace AutoMinesweeper.Services;
public class MinesweeperSolveService
{
    private readonly IGameLayout _gameLayout;
    private readonly GameInteractor _gameInteractor;

    private CellValue[,] _mineMatrix;
    private readonly List<CellPosition> _availableCells = [];

    public bool IsOpenRandomCell { get; set; }

    public bool IsAutoResetGame { get; set; }

    public async Task SolveAsync()
    {
        _gameInteractor.UpdateGameLayout();

        InitializeMineMatrix();
        await _gameInteractor.NewGameAsync();

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

                    isGameWin = await _gameInteractor.IsGameWinAsync();
                    if (isGameWin)
                    {
                        break;
                    }

                    if (IsOpenRandomCell)
                    {
                        await OpenRandomCellAsync();
                        var isGameLose = await _gameInteractor.IsGameLoseAsync();

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

            isGameWin = await _gameInteractor.IsGameWinAsync();
        } while (!isGameWin);
    }

    private void InitializeMineMatrix()
    {
        _mineMatrix = new CellValue[_gameLayout.NumRow, _gameLayout.NumCol];
        for (var i = 0; i < _gameLayout.NumRow; i++)
        {
            for (var j = 0; j < _gameLayout.NumCol; j++)
            {
                _mineMatrix[i, j] = CellValue.Unrevealed;
            }
        }

        _availableCells.Clear();
        for (var rowIndex = 0; rowIndex < _gameLayout.NumRow; rowIndex++)
        {
            for (var colIndex = 0; colIndex < _gameLayout.NumCol; colIndex++)
            {
                var cellPos = CellPosition.GetCellPosition(rowIndex, colIndex);
                _availableCells.Add(cellPos);
            }
        }
    }

    [SupportedOSPlatform("windows5.0")]
    private async Task SolveLevel1Async()
    {
        bool hasOpened;

        var (numRow, numCol) = _gameLayout.GetDimension();

        do
        {
            hasOpened = false;

            SetFlagLevel1();

            for (var rowIndex = 0; rowIndex < numRow; rowIndex++)
            {
                for (var colIndex = 0; colIndex < numCol; colIndex++)
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
        var (numRow, numCol) = _gameLayout.GetDimension();

        for (var rowIndex1 = 0; rowIndex1 < numRow; rowIndex1++)
        {
            for (var colIndex1 = 0; colIndex1 < numCol; colIndex1++)
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
        var (numRow, numCol) = _gameLayout.GetDimension();

        for (var rowIndex1 = 0; rowIndex1 < numRow; rowIndex1++)
        {
            for (var colIndex1 = 0; colIndex1 < numCol; colIndex1++)
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
    private async Task OpenCellAroundAsync(CellPosition centerCellPos)
    {
        var (centerRowIndex, centerColIndex) = centerCellPos.GetIndexes();

        for (var rowIndex = centerRowIndex - 1; rowIndex <= centerRowIndex + 1; rowIndex++)
        {
            for (var colIndex = centerColIndex - 1; colIndex <= centerColIndex + 1; colIndex++)
            {
                var cellPos = CellPosition.GetCellPosition(rowIndex, colIndex);

                if (IsValidCell(cellPos) &&
                    !IsSameCell(cellPos, centerCellPos) &&
                    GetCellValue(cellPos) == CellValue.Unrevealed)
                {
                    await _gameInteractor.OpenCellAsync(cellPos);
                }
            }
        }
    }

    [SupportedOSPlatform("windows5.0")]
    private async ValueTask UpdateCellValueAsync(CellPosition cellPos)
    {
        if (GetCellValue(cellPos) == CellValue.Unrevealed)
        {
            var cellValue = await _gameInteractor.ObtainCellColorAsync(cellPos);
            SetValue(cellPos, cellValue);
            if (cellValue != CellValue.Unrevealed)
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
            if (isOpened = GetCellValue(rCellPosition) != CellValue.Flag)
            {
                await _gameInteractor.OpenCellAsync(rCellPosition);
            }
        } while (!isOpened);
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
                    GetCellValue(cellPos) == CellValue.Unrevealed &&
                    !IsAdjacentCells(cellPos, cellPos2))
                {
                    await _gameInteractor.OpenCellAsync(cellPos);
                }
            }
        }
    }

    [SupportedOSPlatform("windows5.0")]
    private async Task StartRandomCellOpeningAsync()
    {
        while (!ShouldPauseOpenRandomCell())
        {
            await OpenRandomCellAsync();
            var isGameLose = await _gameInteractor.IsGameLoseAsync();
            if (isGameLose)
            {
                InitializeMineMatrix();
                await _gameInteractor.NewGameAsync();
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
                    GetCellValue(cellPos) == CellValue.Unrevealed &&
                    !IsAdjacentCells(cellPos, cellPos2))
                {
                    SetFlag(cellPos);
                }
            }
        }
    }

    private void SetFlagLevel1()
    {
        int nUnopenedCellsAround;

        for (var rowIndex = 0; rowIndex < _gameLayout.NumRow; rowIndex++)
        {
            for (var colIndex = 0; colIndex < _gameLayout.NumCol; colIndex++)
            {
                var cellPos = CellPosition.GetCellPosition(rowIndex, colIndex);

                if (!IsNullCellOrFlagCell(cellPos, out int cellValue))
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

    private void SetFlagAround(CellPosition centerCellPos)
    {
        for (var rowIndex = centerCellPos.RowIndex - 1; rowIndex <= centerCellPos.RowIndex + 1; rowIndex++)
        {
            for (var colIndex = centerCellPos.ColIndex - 1; colIndex <= centerCellPos.ColIndex + 1; colIndex++)
            {
                var cellPos = CellPosition.GetCellPosition(rowIndex, colIndex);

                if (IsValidCell(cellPos) &&
                    GetCellValue(cellPos) == CellValue.Unrevealed)
                {
                    SetFlag(cellPos);
                }
            }
        }
    }

    private bool ShouldPauseOpenRandomCell()
    {
        for (var rowIndex = 0; rowIndex < _gameLayout.NumRow; rowIndex++)
        {
            for (var colIndex = 0; colIndex < _gameLayout.NumCol; colIndex++)
            {
                var cellPos = CellPosition.GetCellPosition(rowIndex, colIndex);

                if (GetCellValue(cellPos) == CellValue.Unrevealed)
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
            IsNullCellOrFlagCell(cellPos, out int cellValue) &&
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

    private bool IsValidCell(CellPosition cellPos)
    {
        return cellPos.RowIndex >= 0 &&
            cellPos.RowIndex < _gameLayout.NumRow &&
            cellPos.ColIndex >= 0 &&
            cellPos.ColIndex < _gameLayout.NumCol;
    }

    private CellValue GetCellValue(CellPosition cellPos)
    {
        return _mineMatrix[cellPos.RowIndex, cellPos.ColIndex];
    }

    private bool IsNullCellOrFlagCell(CellPosition cellPos)
    {
        return GetCellValue(cellPos) <= 0;
    }

    private bool IsNullCellOrFlagCell(CellPosition cellPos, out CellValue cellValue)
    {
        cellValue = GetCellValue(cellPos);
        return cellValue <= 0;
    }

    private bool IsNullCellOrFlagCell(CellPosition cellPos, out int cellValue)
    {
        cellValue = (int)GetCellValue(cellPos);
        return cellValue <= 0;
    }

    private void SetValue(CellPosition cellPos, CellValue value)
    {
        _mineMatrix[cellPos.RowIndex, cellPos.ColIndex] = value;
    }

    private void SetFlag(CellPosition cellPos)
    {
        SetValue(cellPos, CellValue.Flag);
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
                    GetCellValue(cellPos) == CellValue.Unrevealed &&
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
                    GetCellValue(cellPos) == CellValue.Flag &&
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
                    GetCellValue(cellPos) == CellValue.Unrevealed)
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

    private static bool IsSameCell(CellPosition cellPos1, CellPosition cellPos2)
    {
        return cellPos1.RowIndex == cellPos2.RowIndex &&
            cellPos1.ColIndex == cellPos2.ColIndex;
    }
}
