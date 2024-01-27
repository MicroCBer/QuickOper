
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using GameOverlay.Drawing;
using Font = GameOverlay.Drawing.Font;
using Graphics = GameOverlay.Drawing.Graphics;
using Point = GameOverlay.Drawing.Point;
using Rectangle = GameOverlay.Drawing.Rectangle;
using SolidBrush = GameOverlay.Drawing.SolidBrush;

namespace QuickOper
{
    internal class DrawCtx
    {
        private Font font = null;
        private Graphics gfx;
        private Dictionary<string, SolidBrush> brushDictionary;

        public DrawCtx(Font font, Graphics gfx, Dictionary<string, SolidBrush> brushDictionary)
        {
            this.font = font;
            this.gfx = gfx;
            this.brushDictionary = brushDictionary;
        }
        public Point DrawText(string text, float fontsize, string brush, float x, float y, string brushBg = "transparent", float paddingX = 0, float paddingY = 0, float marginX = 0, float marginY = 0, float radius = 0, float maxWidth = float.MaxValue)
        {
            // Calculate available width considering padding and margin
            float availableWidth = maxWidth - (paddingX + marginX) * 2;

            // Split text into lines based on available width
            List<string> lines = MeasureAndSplitText(gfx, font, fontsize, text, availableWidth);

            float totalTextHeight = lines.Count * gfx.MeasureString(font, fontsize, "I").Y; // Assuming "I" has max height

            // Calculate total text width
            float maxLineWidth = 0;
            
            if (lines.Count == 1)
            {
                float lineWidth = gfx.MeasureString(font, fontsize, lines[0]).X;
                maxLineWidth = lineWidth;
            }
            else
            {
                maxLineWidth = maxWidth;
            }

            // Calculate total background area size considering padding and margin
            float bgWidth = maxLineWidth + paddingX * 2;
            float bgHeight = totalTextHeight + paddingY * 2;

            // Draw background if brushBg is not transparent
            if (brushBg != "transparent")
            {
                gfx.FillRoundedRectangle(brushDictionary[brushBg], new RoundedRectangle(
                    Rectangle.Create(x + marginX, y + marginY, bgWidth, bgHeight), radius 
                    ));
            }

            // Draw text
            float currentY = y + paddingY + marginY;
            foreach (string line in lines)
            {
                gfx.DrawText(font, fontsize, brushDictionary[brush], x + paddingX + marginX, currentY, line);
                currentY += gfx.MeasureString(font, fontsize, line).Y; // Move to the next line
            }

            // Calculate the size of the entire text block
            Point pt = new Point((int)bgWidth + marginX * 2, (int)bgHeight + marginY * 2);
            return pt;
        }

        // Function to measure and split text into lines based on available width
        private List<string> MeasureAndSplitText(Graphics gfx, Font font, float fontSize, string text, float availableWidth)
        {
            List<string> lines = new List<string>();
            string[] words = text.Split(' ');
            string currentLine = "";

            foreach (string word in words)
            {
                string testLine = currentLine + word + " ";
                float testWidth = gfx.MeasureString(font, fontSize, testLine).X;

                if (testWidth > availableWidth)
                {
                    lines.Add(currentLine);
                    currentLine = word + " ";
                }
                else
                {
                    currentLine = testLine;
                }

                while (currentLine.Contains("\n"))
                {
                    var splited = currentLine.Split("\n");
                    lines.Add(splited[0]);
                    currentLine = string.Join("\n", splited.Skip(1));
                }
            }

            if (currentLine.Length > 0)
            {
                lines.Add(currentLine);
            }

            return lines;
        }

        public Point DrawText(string text, float fontsize, string brush, ref float x, float y, string brushBg = "transparent", float paddingX = 0, float paddingY = 0, float marginX = 0, float marginY = 0, float radius = 0, float maxWidth = float.MaxValue)
        {
            Point p = DrawText(text, fontsize, brush, x, y, brushBg, paddingX, paddingY, marginX, marginY, radius, maxWidth);
            x += p.X;
            return p;
        }
        public Point DrawText(string text, float fontsize, string brush, float x, ref float y, string brushBg = "transparent", float paddingX = 0, float paddingY = 0, float marginX = 0, float marginY = 0, float radius = 0, float maxWidth = float.MaxValue)
        {
            Point p = DrawText(text, fontsize, brush, x, y, brushBg, paddingX, paddingY, marginX, marginY, radius, maxWidth);
            y += p.Y;
            return p;
        }
    }
}
