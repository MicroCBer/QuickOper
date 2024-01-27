using System.Runtime.InteropServices;
using GameOverlay.Windows;
using GameOverlay.Drawing;
using Graphics = GameOverlay.Drawing.Graphics;
using System.Text;
using QuickOper;
using QuickOper.Commands;
using Rectangle = GameOverlay.Drawing.Rectangle;
using Timer = System.Threading.Timer;


GameOverlay.TimerService.EnableHighPrecisionTimers();

var showing = false;
var lastShowing = !showing;



Command[] commands = null;

var initCommands = () =>
{
    commands = new Command[]
    {
        new Taskkill(),
        new TopWin(),
        new KillWin(),
        new RestartWin(),
        new QueryWord(),
        new ShellCmd(),
        new ShellPwsh(),
        new ShellNode(),
    };
};
initCommands();

var gfx = new Graphics() {
    MeasureFPS = true,
    PerPrimitiveAntiAliasing = true,
    TextAntiAliasing = true
};

var _window = new GraphicsWindow(0, 0, 800, 600, gfx)
{
    FPS = 60,
    IsTopmost = true,
    IsVisible = true
};


Dictionary<string, GameOverlay.Drawing.SolidBrush> _brushes = new();
GameOverlay.Drawing.Font font = null;

var currentInput = "";

var currentCommands = new List<MatchedCommand>();



[DllImport("user32.dll")]
static extern bool SetForegroundWindow(IntPtr hWnd);
[DllImport("user32")]
 static extern
    bool GetMessage(ref Message lpMsg, IntPtr handle, uint mMsgFilterInMain, uint mMsgFilterMax);

_window.SetupGraphics += ((sender, eventArgs) =>
{
    _brushes["black"] = gfx.CreateSolidBrush(0, 0, 0);
    _brushes["white"] = gfx.CreateSolidBrush(255, 255, 255);
    _brushes["red"] = gfx.CreateSolidBrush(255, 0, 0);
    _brushes["green"] = gfx.CreateSolidBrush(85, 177, 85);
    _brushes["blue"] = gfx.CreateSolidBrush(83, 155, 245);
    _brushes["background"] = gfx.CreateSolidBrush(0,0,0,0);
    _brushes["gray-black"] = gfx.CreateSolidBrush(0,0,0, 0.7f);
    _brushes["random"] = gfx.CreateSolidBrush(0, 0, 0);
    _brushes["dark-purple"] = gfx.CreateSolidBrush(82, 66, 78);
    _brushes["purple"] = gfx.CreateSolidBrush(155, 89, 182);
    font = gfx.CreateFont("Consolas", 1);
});

_window.DrawGraphics += ((sender, e) =>
{
    var gfx = e.Graphics;
    var drawCtx = new DrawCtx(font, gfx, _brushes);

    var currentY = 25f;

    gfx.ClearScene(_brushes["background"]);
    
    gfx.FillRectangle(_brushes["white"], Rectangle.Create(0,0,10,18));
    gfx.DrawTextWithBackground(font,18, _brushes["white"], _brushes["black"], 10, 0, "> " + currentInput);

    int displayedCommandCount = 0;

    lock (currentCommands)
    {
        foreach (var command in currentCommands)
        {
            var prefix = command.matchedPrefix;
            var textMatch = currentInput.Substring(0, Math.Min(prefix.Length, currentInput.Length));
            var lenMatched = gfx.MeasureString(font, 16, textMatch);
            var lenFull = gfx.MeasureString(font, 16, prefix);
            gfx.FillRectangle(_brushes["white"], Rectangle.Create(10, currentY, lenFull.X + 3, 20));
            gfx.DrawText(font, 16, _brushes["black"], 13, currentY, prefix);
            gfx.FillRectangle(_brushes["black"], Rectangle.Create(10, currentY, lenMatched.X + 3, 20));
            gfx.DrawText(font, 16, _brushes["white"], 13, currentY, textMatch);
            currentY += 16 + 4;

            gfx.DrawTextWithBackground(font, 13, _brushes["white"], _brushes["black"], 13, currentY, "# " + command.command.GetDescription());
            currentY += 13 + 2;

            if (currentCommands.Count == 1)
            {
                command.command.DrawContent(drawCtx, ref currentInput, ref currentY);
            }

            currentY += 6;
        }
    }
});

_window.DestroyGraphics += (sender, eventArgs) =>
{

};

_window.Create();

Task.Run(() =>
{
    GlobalKeyboardHook kbdHook = new();
    var updateMatchedCommands = () =>
    {
        currentCommands.Clear();
        foreach (var command in commands)
        {
            foreach (var prefix in command.GetPrefix())
            {
                if (prefix.StartsWith(currentInput))
                {
                    currentCommands.Add(new MatchedCommand()
                    {
                        command = command,
                        matchedPrefix = prefix
                    });
                    break;
                }

                if (currentInput.StartsWith(prefix + ' '))
                {
                    currentCommands.Clear();
                    currentCommands.Add(new MatchedCommand()
                    {
                        command = command,
                        matchedPrefix = prefix
                    });
                    return;
                }
            }
        }
    };

    var updateInput = () =>
    {
        lock (currentCommands)
        {
            updateMatchedCommands();
            Task.Run(() =>
            {
                foreach (var matchedCommand in currentCommands.Take(10))
                {
                    matchedCommand.command.InputUpdated(ref currentInput);
                }
            });
        }
    };

    var checkShowing = () =>
    {
        if (showing == lastShowing) return;
        lastShowing = showing;
        if (showing)
        {
            _window.X = Cursor.Position.X;
            _window.Y = Cursor.Position.Y;
            currentInput = "";
            initCommands();
            updateInput();
            _window.Unpause();
            _window.Show();
        }
        else
        {
            _window.Pause();
            _window.Hide();
            currentCommands.Clear();
        }
    };

    var holdingShift = false;
    kbdHook.KeyboardPressed += (s, e) =>
    {
        if (e.KeyboardState == GlobalKeyboardHook.KeyboardState.KeyDown)
        {
            if (showing)
            {
                foreach (var matchedCommand in currentCommands)
                {
                    matchedCommand.command.KeyPress(e.KeyboardData.Key, ref currentInput, ref showing);
                    checkShowing();
                    if (!showing) return;
                }
            }
        }

        if (e.KeyboardState == GlobalKeyboardHook.KeyboardState.KeyDown &&
            (e.KeyboardData.Key == Keys.LShiftKey || e.KeyboardData.Key == Keys.RShiftKey))
        {
            holdingShift = true;
            return;
        }
        else if (e.KeyboardState == GlobalKeyboardHook.KeyboardState.KeyUp &&
                 (e.KeyboardData.Key == Keys.LShiftKey || e.KeyboardData.Key == Keys.RShiftKey))
        {
            holdingShift = false;
            return;
        }

        if (e.KeyboardState == GlobalKeyboardHook.KeyboardState.KeyDown)
        {
            if (showing)
            {
                if (e.KeyboardData.Key == Keys.Back)
                {
                    currentInput = currentInput.Substring(0, Math.Max(currentInput.Length - 1, 0));
                    updateInput();
                }
                else if (e.KeyboardData.Key == Keys.Enter)
                {
                    if(currentCommands.Count > 0) currentCommands[0].command.Submit(ref currentInput, ref showing);
                }
                else if (e.KeyboardData.Key == Keys.Right || e.KeyboardData.Key == Keys.Tab)
                {
                    if (currentCommands.Count != 0)
                    {
                        currentInput = currentCommands[0].matchedPrefix + ' ';
                        updateInput();
                    }
                }
                else
                {
                    string KeyCodeToUnicode(Keys key)
                    {
                        byte[] keyboardState = new byte[255];
                        bool keyboardStateStatus = GetKeyboardState(keyboardState);

                        if (holdingShift) keyboardState[(int)Keys.ShiftKey] = 0xff;

                        if (!keyboardStateStatus)
                        {
                            return "";
                        }

                        uint virtualKeyCode = (uint)key;
                        uint scanCode = MapVirtualKey(virtualKeyCode, 0);
                        IntPtr inputLocaleIdentifier = GetKeyboardLayout(0);

                        StringBuilder result = new StringBuilder();
                        ToUnicodeEx(virtualKeyCode, scanCode, keyboardState, result, (int)5, (uint)0, inputLocaleIdentifier);

                        return result.ToString();
                    }

                    [DllImport("user32.dll")]
                    static extern bool GetKeyboardState(byte[] lpKeyState);

                    [DllImport("user32.dll")]
                    static extern uint MapVirtualKey(uint uCode, uint uMapType);

                    [DllImport("user32.dll")]
                    static extern IntPtr GetKeyboardLayout(uint idThread);

                    [DllImport("user32.dll")]
                    static extern int ToUnicodeEx(uint wVirtKey, uint wScanCode, byte[] lpKeyState, [Out, MarshalAs(UnmanagedType.LPWStr)] StringBuilder pwszBuff, int cchBuff, uint wFlags, IntPtr dwhkl);

                    var key = e.KeyboardData.Key;

                    currentInput += KeyCodeToUnicode(key);

                    while (currentInput.EndsWith("  "))
                    {
                        currentInput = currentInput.Substring(0, currentInput.Length - 1);
                    }
                    updateInput();
                }

                e.Handled = true;
            }

            if (e.KeyboardData.Key == Keys.F9)
            {
                e.Handled = true;
                showing = !showing;
            }

            if (e.KeyboardData.Key == Keys.Escape && showing)
            {
                e.Handled = true;
                showing = false;
            }

            checkShowing();
        }
    };
    Message msg = new Message();
    while (GetMessage(ref msg, IntPtr.Zero, 0, 0))
    {

    }
});


_window.Join();

