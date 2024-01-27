using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Graphics = GameOverlay.Drawing.Graphics;

namespace QuickOper.Commands
{
    abstract class Command
    {
        public abstract string[] GetPrefix();
        public abstract string GetDescription();
        public abstract void InputUpdated(ref string input);
        public abstract void DrawContent(DrawCtx gfx, ref string cmd, ref float startY);
        public abstract void Submit(ref string cmd, ref bool showing);
        public abstract void KeyPress(Keys key, ref string cmd, ref bool showing);
    }

    class MatchedCommand
    {
        public Command command;
        public string matchedPrefix;
    };
}
