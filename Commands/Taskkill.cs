using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;
using Graphics = GameOverlay.Drawing.Graphics;

namespace QuickOper.Commands
{
    internal class Taskkill : Command
    {
        public override void KeyPress(Keys key, ref string cmd, ref bool showing)
        {

        }

        private List<Process> matchingProcesses = new();
        private bool loading = true;
        private string latestKwd = null;

        void UpdateProcessList(string queryWord)
        {
            lock (matchingProcesses)
            {
                if (latestKwd != queryWord) return;
                Task.Run(() =>
                {
                    if (latestKwd != queryWord) return;
                    if (queryWord.Length == 0)
                        matchingProcesses = Process.GetProcesses().ToList();
                    else
                        matchingProcesses = Process.GetProcesses()
                            .Where(p => p.ProcessName.ToLower().Contains(queryWord.ToLower()))
                            .ToList();
                    loading = false;
                    if (latestKwd != queryWord) return;
                    matchingProcesses = matchingProcesses.OrderByDescending(v => v.WorkingSet64).ToList();
                });
            }
        }
        public override void InputUpdated(ref string cmd)
        {
            loading = true;
            killState = KillState.None;
            var queryWord = string.Join(" ", cmd.Split(" ").Skip(1).Where(v => !v.StartsWith('-')));
            if (latestKwd == queryWord) return;
            latestKwd = queryWord;
            UpdateProcessList(queryWord);
        }

        public override string GetDescription()
        {
            return "进程管理";
        }

        public override string[] GetPrefix()
        {
            return new[]
            {
                "top","taskkill", "tk", "kill", "stop"
            };
        }

        enum KillState
        {
            None,
            Selected,
            TooMany,
            Pending,
            Complete
        };
        private KillState killState;
        public override void DrawContent(DrawCtx gfx, ref string cmd, ref float startY)
        {
            if (matchingProcesses.Count == 0)
            {
                gfx.DrawText(loading ? "加载中" : "无相关进程", 15, "white", 10, ref startY, "dark-purple", paddingX: 10, paddingY: 5);
            }
            else
            {
                if(killState == KillState.Selected) gfx.DrawText($"点击 Enter 以确定杀死 {matchingProcesses.Count} 个进程", 15, "white", 10, ref startY, "red", paddingX: 10, paddingY: 5);
                if (killState == KillState.TooMany) gfx.DrawText($"要杀死的进程过多，无法执行操作", 15, "white", 10, ref startY, "red", paddingX: 10, paddingY: 5);
                if (killState == KillState.Pending) gfx.DrawText($"正在执行...", 15, "white", 10, ref startY, "red", paddingX: 10, paddingY: 5);
                if (killState == KillState.Complete) gfx.DrawText($"已完成，重新检索或点击 Enter 关闭工具", 15, "white", 10, ref startY, "green", paddingX: 10, paddingY: 5);
                gfx.DrawText($"找到 {matchingProcesses.Count} 个进程：", 15, "white", 10, ref startY, "dark-purple",
                    paddingX: 10, paddingY: 5);

                foreach (var matchingProcess in matchingProcesses)
                {
                    var x = 10f;
                    startY += 4;
                    gfx.DrawText(matchingProcess.Id.ToString(), 12, "white", ref x, startY, "black");
                    var deltaY = gfx.DrawText(matchingProcess.ProcessName, 16, "white", ref x, startY - 4, "blue", paddingX: 5).Y;
                    gfx.DrawText($"MEM {(matchingProcess.WorkingSet64 / (1024*1024)).ToString("F1")}M", 12, "white", ref x, startY, "purple");
                    gfx.DrawText(matchingProcess.MainWindowTitle, 12, "white", ref x, startY, "gray-black");

                    startY += deltaY + 3;

                    if (startY > 700) break;
                }
            }
        }

        public override void Submit(ref string cmd, ref bool showing)
        {
            if (killState == KillState.None)
            {
                if (matchingProcesses.Count > 60)
                {
                    killState = KillState.TooMany;
                }
                else

                {
                    killState = KillState.Selected;
                }
            } else if (killState == KillState.Selected)
            {
                killState = KillState.Pending;

                foreach (var process in matchingProcesses)
                {
                    try
                    {
                        process.Kill();
                        Thread.Sleep(30);
                        UpdateProcessList(latestKwd);
                    }
                    catch (Win32Exception e)
                    {

                    }
                }

                killState = KillState.Complete;
            } else if (killState == KillState.Complete)
            {
                showing = false;
            }
        }
    }
}