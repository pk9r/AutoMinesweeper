using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace AutoMinesweeper
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window, INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;
        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChangedEventHandler handler = PropertyChanged;
            if (handler != null)
                handler(this, new PropertyChangedEventArgs(propertyName));
        }

        #region Constants
        const int CELL_SIZE = 16;
        const int BASE_CELL_X = 12;
        const int BASE_CELL_Y = 55;
        const int OFFSET_WIDTH = 26;
        const int OFFSET_HEIGHT = 112;
        const int OFFSET_GAME_LOSE_X = 5;
        const int GAME_LOSE_Y = 32;
        const int OFFSET_GAMEWIN_X = 8;
        const int GAME_WIN_Y = 28;
        const int OFFSET_COLOR_CELL_X = 9;
        const int OFFSET_COLOR_CELL_Y = 12;
        const string TITLE_GAME = "Minesweeper";
        const string CLASS_GAME = "Minesweeper";
        const int CELL_NULL = -2;
        const int CELL_FLAG = -1;
        const int COLOR_0 = 12632256;
        const int COLOR_1 = 255;
        const int COLOR_2 = 32768;
        const int COLOR_3 = 16711680;
        const int COLOR_4 = 128;
        const int COLOR_5 = 8388608;
        const int COLOR_6 = 32896;
        const int COLOR_7 = 0;
        const int K_F2 = 113;
        #endregion

        private bool HasHwndGame;
        private int NumRow;
        private int NumColumn;
        private int GameLoseX;
        private int GameLoseY;
        private int GameWinX;
        private int GameWinY;
        private int[,] MineMatrix;
        private bool _IsEnableWindow;
        private bool _IsAutoResetGame;
        private bool _IsOpenRandomCell;
        private bool _HasImageSearch2020;
        private int _TimeDelayOpenCell;

        private List<int[]> CellRandom = new List<int[]>();
        private IntPtr GameHwnd = IntPtr.Zero;
        private static readonly Random Rand = new Random();

        public MainWindow()
        {
            InitializeComponent();
            DataContext = this;
            IsEnableWindow = true;
            IsAutoResetGame = true;
            IsOpenRandomCell = true;
            HasImageSearch2020 = false;
        }

        public bool IsEnableWindow 
        {
            get => _IsEnableWindow;
            set
            {
                _IsEnableWindow = value;
                OnPropertyChanged();
            }
        }
        public bool IsOpenRandomCell
        {
            get => _IsOpenRandomCell;
            set
            {
                _IsOpenRandomCell = value;
                if (!_IsOpenRandomCell)
                {
                    IsAutoResetGame = false;
                }
                OnPropertyChanged();
            }
        }
        public bool IsAutoResetGame
        {
            get => _IsAutoResetGame;
            set
            {
                _IsAutoResetGame = value;
                OnPropertyChanged();
            }
        }
        public int TimeDelayOpenCell
        {
            get => _TimeDelayOpenCell;
            set
            {
                _TimeDelayOpenCell = value;
                OnPropertyChanged();
            }
        }
        public bool HasImageSearch2020
        {
            get => _HasImageSearch2020;
            set
            {
                _HasImageSearch2020 = value;
                if (_HasImageSearch2020)
                {
                    TimeDelayOpenCell = 2;
                }
                else
                {
                    TimeDelayOpenCell = 7;
                }    
                OnPropertyChanged();
            }
        }
        private bool IsGameLose => GetPixel(TITLE_GAME, GameHwnd, GameLoseX, GameLoseY) == 0;
        private bool IsGameWin => GetPixel(TITLE_GAME, GameHwnd, GameWinX, GameWinY) == 0;
        private bool ShouldPauseOpenRandomCell
        {
            get
            {
                for (int i = 0; i < NumRow; i++)
                {
                    for (int j = 0; j < NumColumn; j++)
                    {
                        if (MineMatrix[i, j] == 0)
                        {
                            return true;
                        }
                    }
                }
                return false;
            }
        }

        private bool SetHwndGame()
        {
            GameHwnd = Control.FindWindowHandle(CLASS_GAME, TITLE_GAME);
            return GameHwnd != IntPtr.Zero;
        }
        private void GetSizeGame()
        {
            Control.RECT lpRect = Control.GetWindowRect(GameHwnd);
            NumRow = (lpRect.Bottom - lpRect.Top - OFFSET_HEIGHT) / (CELL_SIZE);
            NumColumn = (lpRect.Right - lpRect.Left - OFFSET_WIDTH) / (CELL_SIZE);
            GameLoseX = (lpRect.Right - lpRect.Left) / 2 - OFFSET_GAME_LOSE_X;
            GameLoseY = GAME_LOSE_Y;
            GameWinX = (lpRect.Right - lpRect.Left) / 2 - OFFSET_GAMEWIN_X;
            GameWinY = GAME_WIN_Y;
        }
        private void ResetCellRandom()
        {
            CellRandom.Clear();
            for (int i = 0; i < NumRow; i++)
            {
                for (int j = 0; j < NumColumn; j++)
                {
                    int[] vs = new int[] { i, j };
                    CellRandom.Add(vs);
                }
            }
        }
        private bool ResetDataGame()
        {
            if (!HasHwndGame || !Control.IsWindowVisible(GameHwnd))
            {
                if (!SetHwndGame())
                {
                    var result = MessageBox.Show("Không tìm thấy cửa sổ game. Thử lại?", "Lỗi", MessageBoxButton.OKCancel);
                    if (result == MessageBoxResult.OK)
                    {
                        return ResetDataGame();
                    }
                    return false;
                }
                HasHwndGame = true;
            }
            GetSizeGame();
            MineMatrix = new int[NumRow, NumColumn];
            for (int i = 0; i < NumRow; i++)
            {
                for (int j = 0; j < NumColumn; j++)
                {
                    MineMatrix[i, j] = CELL_NULL;
                }
            }
            ResetCellRandom();
            return true;
        }
        private int GetPixel(string title, IntPtr hWnd, int x, int y)
        {
            if (HasImageSearch2020)
            {
                return Control.GetPixelFromWindow("title", title, x, y);
            }
            return Control.GetPixelFromWindow(hWnd, x, y);
        }

        private int GetValueCell(int i, int j)
        {
            int x = BASE_CELL_X + j * (CELL_SIZE) + OFFSET_COLOR_CELL_X;
            int y = BASE_CELL_Y + i * (CELL_SIZE) + OFFSET_COLOR_CELL_Y;
            int color = GetPixel(TITLE_GAME, GameHwnd, x, y);
            switch (color)
            {
                case COLOR_0:
                    x = BASE_CELL_X + j * (CELL_SIZE) + 1;
                    y = BASE_CELL_Y + i * (CELL_SIZE) + 1;
                    if (GetPixel(TITLE_GAME, GameHwnd, x, y) != COLOR_0)
                    {
                        return CELL_NULL;
                    }
                    return 0;
                case COLOR_1:
                    return 1;
                case COLOR_2:
                    return 2;
                case COLOR_3:
                    return 3;
                case COLOR_4:
                    return 4;
                case COLOR_5:
                    return 5;
                case COLOR_6:
                    return 6;
                case COLOR_7:
                    return 7;
                default:
                    return 8;
            }
        }
        private void UpdateDataCell(int i, int j)
        {
            if (MineMatrix[i, j] == CELL_NULL)
            {
                MineMatrix[i, j] = GetValueCell(i, j);
                if ((MineMatrix[i, j] != CELL_NULL))
                {
                    CellRandom.Remove(new int[] { i, j });
                    if (MineMatrix[i, j] == 0)
                    {
                        UpdateCellAround(i, j);
                    }
                    return;
                }
            }
        }
        private void UpdateCellAround(int i, int j)
        {
            for (int a = i - 1; a <= i + 1; a++)
            {
                for (int b = j - 1; b <= j + 1; b++)
                {
                    if (IsValidCell(a, b) && (a != i || b != j))
                    {
                        UpdateDataCell(a, b);
                    }
                }
            }
        }
        private void OpenRandomCell()
        {
            int index = Rand.Next(0, CellRandom.Count - 1);
            int[] vs = CellRandom.ElementAt(index);
            OpenCell(vs[0], vs[1]);
        }
        private void OpenRandomCells()
        {
            while (!ShouldPauseOpenRandomCell)
            {
                OpenRandomCell();
                if (IsGameLose)
                {
                    NewGame();
                }
            }
        }
        private void OpenCell(int i, int j)
        {
            int x = BASE_CELL_X + j * (CELL_SIZE) + (CELL_SIZE) / 2;
            int y = BASE_CELL_Y + i * (CELL_SIZE) + (CELL_SIZE) / 2;
            Control.ControlClick(GameHwnd, x, y);
            Thread.Sleep(TimeDelayOpenCell);
            UpdateDataCell(i, j);
        }
        private void OpenCellAround(int i, int j)
        {
            for (int a = i - 1; a <= i + 1; a++)
            {
                for (int b = j - 1; b <= j + 1; b++)
                {
                    if (IsValidCell(a, b) && !IsOneCell(i, j, a, b) && MineMatrix[a, b] == CELL_NULL)
                    {
                        OpenCell(a, b);
                    }
                }
            }
        }
        private void SetFlagAround(int i, int j)
        {
            for (int a = i - 1; a <= i + 1; a++)
            {
                for (int b = j - 1; b <= j + 1; b++)
                {
                    if (IsValidCell(a, b) && MineMatrix[a, b] == CELL_NULL)
                    {
                        MineMatrix[a, b] = CELL_FLAG;
                    }
                }
            }
        }
        private void SetFlagLevel1()
        {
            int nCellUnopendAround;
            for (int i = 0; i < NumRow; i++)
            {
                for (int j = 0; j < NumColumn; j++)
                {
                    if (MineMatrix[i, j] > 0)
                    {
                        nCellUnopendAround = CountCellUnopendAround(i, j);
                        if (nCellUnopendAround != CountCellFlagAround(i, j) && MineMatrix[i, j] >= nCellUnopendAround)
                        {
                            SetFlagAround(i, j);
                        }
                    }
                }
            }
        }
        private void OpenCellLevel2(int i, int j, int a, int b)
        {
            for (int x = i - 1; x <= i + 1; x++)
            {
                for (int y = j - 1; y <= j + 1; y++)
                {
                    if (IsValidCell(x, y) && !IsTwoCellsAround(x, y, a, b) && MineMatrix[x, y] == CELL_NULL)
                    {
                        OpenCell(x, y);
                    }
                }
            }
        }
        private void SetFlagLevel2(int i, int j, int a, int b)
        {
            for (int x = i - 1; x <= i + 1; x++)
            {
                for (int y = j - 1; y <= j + 1; y++)
                {
                    if (IsValidCell(x, y) && !IsTwoCellsAround(x, y, a, b) && MineMatrix[x, y] == CELL_NULL)
                    {
                        MineMatrix[x, y] = CELL_FLAG;
                    }
                }
            }
        }

        private bool IsValidCell(int i, int j)
        {
            return i >= 0 && i < NumRow && j >= 0 && j < NumColumn;
        }
        private bool IsOneCell(int i, int j, int a, int b)
        {
            return i == a && j == b;
        }
        private bool IsTwoCellsAround(int i, int j, int a, int b)
        {
            return Math.Abs(i - a) <= 1 && Math.Abs(j - b) <= 1;
        }
        private int CountCellUnopendAround(int i, int j)
        {
            int nCount = 0;
            for (int a = i - 1; a <= i + 1; a++)
            {
                for (int b = j - 1; b <= j + 1; b++)
                {
                    if (IsValidCell(a, b) && !IsOneCell(a, b, i, j) && MineMatrix[a, b] < 0)
                    {
                        nCount++;
                    }
                }
            }
            return nCount;
        }
        private int CountCellNullAround(int i, int j)
        {
            int nCount = 0;
            for (int a = i - 1; a <= i + 1; a++)
            {
                for (int b = j - 1; b <= j + 1; b++)
                {
                    if (IsValidCell(a, b) && !IsOneCell(a, b, i, j) && MineMatrix[a, b] == CELL_NULL)
                    {
                        nCount++;
                    }
                }
            }
            return nCount;
        }
        private int CountCellFlagAround(int i, int j)
        {
            int nCount = 0;
            for (int a = i - 1; a <= i + 1; a++)
            {
                for (int b = j - 1; b <= j + 1; b++)
                {
                    if (IsValidCell(a, b) && !IsOneCell(a, b, i, j) && MineMatrix[a, b] == CELL_FLAG)
                    {
                        nCount++;
                    }
                }
            }
            return nCount;
        }
        private int CountCellNullLevel2(int i, int j, int a, int b)
        {
            int nCount = 0;
            for (int x = i - 1; x <= i + 1; x++)
            {
                for (int y = j - 1; y <= j + 1; y++)
                {
                    if (IsValidCell(x, y) && IsTwoCellsAround(x, y, a, b) && MineMatrix[x, y] == CELL_NULL)
                    {
                        nCount++;
                    }
                }
            }
            return nCount;
        }
        private bool CanOpenCellAround(int i, int j)
        {
            int nCellFlagAround = CountCellFlagAround(i, j);
            if (MineMatrix[i, j] == nCellFlagAround && CountCellNullAround(i, j) > 0)
            {
                return true;
            }
            return false;
        }
        private bool CanOpenCellLevel2(int i, int j, int a, int b)
        {
            int nCellNullAround1 = CountCellNullAround(i, j);
            int nCellNullAround2 = CountCellNullAround(a, b);
            int nFlag1 = MineMatrix[i, j] - CountCellFlagAround(i, j);
            int nFlag2 = MineMatrix[a, b] - CountCellFlagAround(a, b);
            int nCellNull = CountCellNullLevel2(i, j, a, b);
            return nCellNullAround1 - nCellNull > 0 && nFlag1 == nFlag2 - nCellNullAround2 + nCellNull;
        }
        private bool CanSetFlagLevel2(int i, int j, int a, int b)
        {
            int nCellNullAround1 = CountCellNullAround(i, j);
            int nFlag1 = MineMatrix[i, j] - CountCellFlagAround(i, j);
            int nFlag2 = MineMatrix[a, b] - CountCellFlagAround(a, b);
            int nCellNull = CountCellNullLevel2(i, j, a, b);
            return nCellNullAround1 - nCellNull > 0 && nFlag1 == nFlag2 + nCellNullAround1 - nCellNull;
        }

        private void SolveLevel1()
        {
            bool hasOpened;
            do
            {
                hasOpened = false;
                SetFlagLevel1();
                for (int i = 0; i < NumRow; i++)
                {
                    for (int j = 0; j < NumColumn; j++)
                    {
                        if (MineMatrix[i, j] != CELL_NULL && CanOpenCellAround(i, j))
                        {
                            OpenCellAround(i, j);
                            hasOpened = true;
                        }
                    }
                }
            } while (hasOpened);
        }
        private bool SolveCellLevel2()
        {
            for (int i = 0; i < NumRow; i++)
            {
                for (int j = 0; j < NumColumn; j++)
                {
                    if (MineMatrix[i, j] > 0)
                    {
                        for (int a = i - 2; a <= i + 2; a++)
                        {
                            for (int b = j - 2; b <= j + 2; b++)
                            {
                                if (IsValidCell(a, b) && !IsOneCell(a, b, i, j) && MineMatrix[a, b] > 0 && CanOpenCellLevel2(i, j, a, b))
                                {
                                    OpenCellLevel2(i, j, a, b);
                                    return true;
                                }
                            }
                        }
                    }
                }
            }
            return false;
        }
        private bool SolveFlagLevel2()
        {
            for (int i = 0; i < NumRow; i++)
            {
                for (int j = 0; j < NumColumn; j++)
                {
                    if (MineMatrix[i, j] > 0)
                    {
                        for (int a = i - 2; a <= i + 2; a++)
                        {
                            for (int b = j - 2; b <= j + 2; b++)
                            {
                                if (a < 0 || a >= NumRow || b < 0 || b >= NumColumn)
                                    continue;
                                if (a == i && b == j)
                                    continue;
                                if (MineMatrix[a, b] > 0 && CanSetFlagLevel2(i, j, a, b))
                                {
                                    SetFlagLevel2(i, j, a, b);
                                    return true;
                                }
                            }
                        }
                    }
                }
            }
            return false;
        }

        private bool NewGame()
        {
            if (ResetDataGame())
            {
                Control.SendKeyBoardDown(GameHwnd, K_F2);
                Thread.Sleep(200);
                return true;
            }
            return false;
        }

        public void AutoPlayGame()
        {
            if (!NewGame())
            {
                return;
            }
            IsEnableWindow = false;
            OpenRandomCells();
            while (!IsGameWin)
            {
                SolveLevel1();
                if (!SolveFlagLevel2() && !SolveCellLevel2())
                {
                    if (IsOpenRandomCell)
                    {
                        OpenRandomCell();
                        if (IsAutoResetGame && IsGameLose)
                        {
                            AutoPlayGame();
                            return;
                        }
                    }
                    else
                    {
                        break;
                    }    
                }
            }
            IsEnableWindow = true;
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            AutoPlayGame();
        }
    }
}
