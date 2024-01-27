using PInvoke;
using SharpDX;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace QuickOper.Commands
{
    internal abstract class Shell:Command
    {
        enum State
        {
            NotUpdated, Executing, Finished
        }
        private State state = State.NotUpdated;
        private string result = "";
        public override void InputUpdated(ref string input)
        {
            state = State.NotUpdated;
        }

        public override void DrawContent(DrawCtx gfx, ref string cmd, ref float startY)
        {
            if (state == State.NotUpdated) gfx.DrawText("按下 Enter 以执行", 15, "white", 10, ref startY, "gray-black", 10);
            else if (state == State.Executing) gfx.DrawText("正在执行...", 15, "white", 10, ref startY, "blue", 10);
            else gfx.DrawText("√ 执行完毕", 13, "white", 10, ref startY, "green", 10);

            startY += 6;

            if(result.Length > 0)
            foreach (string s in result.Split("\n"))
            {
                gfx.DrawText(s, 12, "white", 10, ref startY, "black", 10);
            }
        }

        private string latestCommand = null;
        public override void Submit(ref string cmd, ref bool showing)
        {
            var command = string.Join(" ", cmd.Split(" ").Skip(1));
            if (latestCommand == command) return;
            if (latestCommand == null) result = "";

            state = State.Executing;
            latestCommand = command;
            lock (result)
            {
                if (latestCommand != command) return;
                Task.Run(() =>
                {
                     Execute(command);
                     state = State.Finished;
                     if (latestCommand == command) latestCommand = null;
                });
            }
        }
        public abstract void Execute(string cmd);

        public override void KeyPress(Keys key, ref string cmd, ref bool showing)
        {
        }

        protected void exec(string filename, string args)
        {
            System.Diagnostics.ProcessStartInfo procStartInfo =
                new System.Diagnostics.ProcessStartInfo(filename, args);
            
            procStartInfo.RedirectStandardOutput = true;
            procStartInfo.RedirectStandardError = true;
            procStartInfo.UseShellExecute = false;
            // Do not create the black window.
            procStartInfo.CreateNoWindow = true;
            // Now we create a process, assign its ProcessStartInfo and start it
            System.Diagnostics.Process proc = new System.Diagnostics.Process();
            proc.StartInfo = procStartInfo;
            proc.Start();
            result = "";
            Task.Run(() =>
            {
                string standard_output;
                while ((standard_output = proc.StandardOutput.ReadLine()) != null)
                {
                    result += standard_output + "\n";
                }
            });
            Task.Run(() =>
            {
                string standard_output;
                while ((standard_output = proc.StandardError.ReadLine()) != null)
                {
                    result += standard_output + "\n";

                }
            });
            if (!proc.WaitForExit(10000))
            {
                proc.Kill();
            }
        }
    }

    internal class ShellCmd : Shell
    {
        public override string[] GetPrefix()
        {
            return new string[] { "cmd" };
        }

        public override string GetDescription()
        {
            return "执行 cmd 命令";
        }

        public override void Execute(string cmd)
        {
            exec("CMD.exe", "/c "+cmd);
        }
    }

    internal class ShellPwsh : Shell
    {
        public override string[] GetPrefix()
        {
            return new string[] { "pwsh","powershell","shell" };
        }

        public override string GetDescription()
        {
            return "执行 PowerShell 命令";
        }

        public override void Execute(string cmd)
        {
            exec("PowerShell.exe", "/Command " + cmd);
        }
    }

    internal class ShellNode : Shell
    {
        public override string[] GetPrefix()
        {
            return new string[] { "node", "eval" };
        }

        public override string GetDescription()
        {
            return "执行 Node.JS 命令（需安装 NodeJS）";
        }

        public override void Execute(string cmd)
        {
            exec("node.exe ", $"--eval \"const importToGlobal = (obj)=>{{for(let x in obj) globalThis[x] = obj[x]}}; importToGlobal(require('fs')); (eval(\"\"async ()=>{
                cmd.Replace("\"", "\\\"\"").Replace("\\","\\\\")
            }\"\"))().then(v=>console.log(require('node:util').inspect(v)))\"");
        }
    }
}
