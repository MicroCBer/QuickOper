
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using Graphics = GameOverlay.Drawing.Graphics;

namespace QuickOper.Commands
{
    internal class TopWin : Command
    {
        class WindowHelper
        {
            const uint SWP_NOSIZE = 0x0001;
            const uint SWP_NOMOVE = 0x0002;
            const uint TOPMOST_FLAGS = SWP_NOMOVE | SWP_NOSIZE;

            [DllImport("user32.dll")]
            [return: MarshalAs(UnmanagedType.Bool)]
            private static extern bool SetWindowPos(IntPtr hWnd, IntPtr hWndInsertAfter, int X, int Y, int cx, int cy, uint uFlags);

            [DllImport("user32.dll")]
            private static extern IntPtr GetForegroundWindow();

            public static void ToggleTopMost()
            {
                IntPtr currentForegroundWindow = GetForegroundWindow();
                if (currentForegroundWindow != IntPtr.Zero)
                {
                    bool isTopMost = (GetWindowLong(currentForegroundWindow, -20) & 0x00000008) != 0; // Check if window is topmost

                    if (isTopMost)
                    {
                        SetWindowPos(currentForegroundWindow, IntPtr.Zero, 0, 0, 0, 0, TOPMOST_FLAGS); // Remove topmost
                    }
                    else
                    {
                        SetWindowPos(currentForegroundWindow, new IntPtr(-1), 0, 0, 0, 0, TOPMOST_FLAGS); // Set topmost
                    }
                }
            }

            [DllImport("user32.dll")]
            private static extern int GetWindowLong(IntPtr hWnd, int nIndex);
        }

        public override void InputUpdated(ref string input)
        {
        }

        public override string GetDescription()
        {
            return "置顶/取消置顶当前窗口";
        }

        public override string[] GetPrefix()
        {
            return new[]
            {
                "topwin","toponly", "wintop"
            };
        }

        public override void DrawContent(DrawCtx gfx, ref string cmd, ref float startY)
        {

        }

        public override void Submit(ref string cmd, ref bool showing)
        {
            WindowHelper.ToggleTopMost();
            showing = false;
        }
        public override void KeyPress(Keys key, ref string cmd, ref bool showing)
        {

        }
    }

public class ProcessHelper
    {
        [DllImport("user32.dll")]
        private static extern IntPtr GetForegroundWindow();

        [DllImport("user32.dll", SetLastError = true)]
        private static extern uint GetWindowThreadProcessId(IntPtr hWnd, out uint lpdwProcessId);

        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern IntPtr OpenProcess(uint dwDesiredAccess, bool bInheritHandle, uint dwProcessId);

        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern bool TerminateProcess(IntPtr hProcess, uint uExitCode);

        [DllImport("kernel32.dll")]
        private static extern bool CloseHandle(IntPtr hObject);

        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern IntPtr CreateToolhelp32Snapshot(uint dwFlags, uint th32ProcessID);

        [DllImport("kernel32.dll")]
        private static extern bool Process32First(IntPtr hSnapshot, ref PROCESSENTRY32 lppe);

        [DllImport("kernel32.dll")]
        private static extern bool Process32Next(IntPtr hSnapshot, ref PROCESSENTRY32 lppe);

        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern bool QueryFullProcessImageName(IntPtr hProcess, int dwFlags, [Out] char[] lpExeName, ref int lpdwSize);

        [DllImport("user32.dll", EntryPoint = "FindWindowEx",
            CharSet = CharSet.Auto)]
        static extern IntPtr FindWindowEx(IntPtr hwndParent,
            IntPtr hwndChildAfter, string lpszClass, IntPtr lpszWindow);

        [StructLayout(LayoutKind.Sequential)]
        private struct PROCESSENTRY32
        {
            public uint dwSize;
            public uint cntUsage;
            public uint th32ProcessID;
            public IntPtr th32DefaultHeapID;
            public uint th32ModuleID;
            public uint cntThreads;
            public uint th32ParentProcessID;
            public int pcPriClassBase;
            public uint dwFlags;
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 260)]
            public string szExeFile;
        }

        private const uint PROCESS_TERMINATE = 0x0001;
        private const uint PROCESS_QUERY_LIMITED_INFORMATION = 0x1000;
        private const uint TH32CS_SNAPPROCESS = 0x00000002;

        public static void KillForegroundProcess()
        {
            IntPtr hWnd = GetForegroundWindow();
            uint processId;
            GetWindowThreadProcessId(hWnd, out processId);
            IntPtr hProcess = OpenProcess(PROCESS_TERMINATE, false, processId);

            if (hProcess != IntPtr.Zero)
            {
                TerminateProcess(hProcess, 0);
                CloseHandle(hProcess);
            }
        }

        public static void RestartForegroundProcess()
        {
            IntPtr hWnd = GetForegroundWindow();
            uint processId;
            GetWindowThreadProcessId(hWnd, out processId);
            
            string processPath = GetProcessImagePath(processId);

            if (processPath.Contains("\\ApplicationFrameHost.exe"))
            {
                var hwndCoreWin = FindWindowEx(hWnd, 0, "Windows.UI.Core.CoreWindow", 0);
                GetWindowThreadProcessId(hwndCoreWin, out processId);
                processPath = GetProcessImagePath(processId);
            }
            if (!string.IsNullOrEmpty(processPath))
            {
                IntPtr hProcess = OpenProcess(PROCESS_TERMINATE, false, processId);

                if (hProcess != IntPtr.Zero)
                {
                    TerminateProcess(hProcess, 0);
                    CloseHandle(hProcess);

                    System.Threading.Thread.Sleep(300);

                    Process.Start(processPath);
                }
            }
        }

        private static string GetProcessImagePath(uint processId)
        {
            IntPtr hSnapshot = CreateToolhelp32Snapshot(TH32CS_SNAPPROCESS, 0);
            if (hSnapshot == IntPtr.Zero)
            {
                return null;
            }

            PROCESSENTRY32 pe32 = new PROCESSENTRY32();
            pe32.dwSize = (uint)Marshal.SizeOf(typeof(PROCESSENTRY32));

            if (!Process32First(hSnapshot, ref pe32))
            {
                CloseHandle(hSnapshot);
                return null;
            }

            do
            {
                if (pe32.th32ProcessID == processId)
                {
                    IntPtr hProcess = OpenProcess(PROCESS_QUERY_LIMITED_INFORMATION, false, processId);
                    if (hProcess != IntPtr.Zero)
                    {
                        const int bufferSize = 1024;
                        char[] buffer = new char[bufferSize];
                        int size = bufferSize;
                        QueryFullProcessImageName(hProcess, 0, buffer, ref size);
                        CloseHandle(hProcess);

                        string path = new string(buffer, 0, size);
                        return path;
                    }
                }
            } while (Process32Next(hSnapshot, ref pe32));

            CloseHandle(hSnapshot);
            return null;
        }
    }



    internal class KillWin : Command
    {
        public override void KeyPress(Keys key, ref string cmd, ref bool showing)
        {
            
        }

        enum ConfirmState
        {
            Plain, Selected, Confirmed
        }
        private ConfirmState state = ConfirmState.Plain;
        public override string GetDescription()
        {
            if(state == ConfirmState.Plain)
                return "杀死当前窗口进程";
            else if (state == ConfirmState.Selected)
                return "!! 再次确认以杀死当前窗口进程 !!";
            else return "··· 正在处理 ···";
        }
       
        public override string[] GetPrefix()
        {
            return new[]
            {
                "killwin","winkill"
            };
        }

        public override void DrawContent(DrawCtx gfx, ref string cmd, ref float startY)
        {

        }

        public override void InputUpdated(ref string input)
        {
        }

        public override void Submit(ref string cmd, ref bool showing)
        {
            if (state == ConfirmState.Plain) state = ConfirmState.Selected;
            else if (state == ConfirmState.Selected)
            {
                state = ConfirmState.Confirmed;
                ProcessHelper.KillForegroundProcess();
                showing = false;
            }
        }
    }
    internal class RestartWin : Command
    {
        public static void RestartForegroundProcess()
        {
            IntPtr foregroundWindowHandle = NativeMethods.GetForegroundWindow();
            uint processId;
            NativeMethods.GetWindowThreadProcessId(foregroundWindowHandle, out processId);
            Process process = Process.GetProcessById((int)processId);

            if (!process.HasExited)
            {
                var filename = process.StartInfo.FileName;
                process.Kill();
                Thread.Sleep(500);
                Process.Start(filename);
            }
        }

        private static class NativeMethods
        {
            [System.Runtime.InteropServices.DllImport("user32.dll")]
            public static extern IntPtr GetForegroundWindow();

            [System.Runtime.InteropServices.DllImport("user32.dll")]
            public static extern uint GetWindowThreadProcessId(IntPtr hWnd, out uint lpdwProcessId);
        }

        public override void KeyPress(Keys key, ref string cmd, ref bool showing)
        {

        }

        enum ConfirmState
        {
            Plain, Selected, Confirmed
        }
        private ConfirmState state = ConfirmState.Plain;
        public override string GetDescription()
        {
            if (state == ConfirmState.Plain)
                return "重启当前窗口进程";
            else if (state == ConfirmState.Selected)
                return "!! 再次确认以重启当前窗口进程 !!";
            else return "··· 正在处理 ···";
        }

        public override string[] GetPrefix()
        {
            return new[]
            {
                "restartwin","winrestart"
            };
        }

        public override void DrawContent(DrawCtx gfx, ref string cmd, ref float startY)
        {

        }

        public override void InputUpdated(ref string input)
        {
        }

        public override void Submit(ref string cmd, ref bool showing)
        {
            if (state == ConfirmState.Plain) state = ConfirmState.Selected;
            else if (state == ConfirmState.Selected)
            {
                state = ConfirmState.Confirmed;
                ProcessHelper.RestartForegroundProcess();
                showing = false;
            }
        }
    }
}
