using System.Runtime.Versioning;
using System.Threading.Tasks;

using AutoMinesweeper.Services;

using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace AutoMinesweeper.ViewModels;

public partial class AutoMinesweeperViewModel : ObservableObject
{
    [ObservableProperty]
    private bool _isAutoResetGame;

    [ObservableProperty]
    private bool _isOpenRandomCell;

    [ObservableProperty]
    private bool _hasImageSearch2020;

    [ObservableProperty]
    private int _timeDelayOpenCell = 100;

    private readonly MinesweeperSolveService _minesweeperSolveService;

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
        await _minesweeperSolveService.SolveAsync();
    }
}
