using System.Runtime.Versioning;
using System.Windows;

using AutoMinesweeper.ViewModels;

namespace AutoMinesweeper;

/// <summary>
/// Interaction logic for App.xaml
/// </summary>
[SupportedOSPlatform("windows5.0")]
public partial class App : Application
{
    public static AutoMinesweeperViewModel AutoMinesweeperViewModel { get; } = new ();
}
