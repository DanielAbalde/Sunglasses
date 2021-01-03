using Grasshopper.GUI.Canvas;
using Grasshopper.Kernel; 
using System.Collections.Generic;
using System.Drawing; 

namespace Sunglasses
{

    public static class Utils
    { 
        public static string Key_DrawNames => "Sunglasses.DrawNames";
        public static string Key_FontSize => "Sunglasses.FontSize";
        public static string Key_DisplayNicknames => "Sunglasses.DisplayNicknames";
        public static string Key_FilterComponents => "Sunglasses.FilterComponents";
        public static string Key_FilterParameters => "Sunglasses.FilterParameters";
        public static string Key_FilterSpecial => "Sunglasses.FilterSpecial";
        public static string Key_FilterGraphic => "Sunglasses.FilterGraphic";
        public static string Key_FilterCustom => "Sunglasses.FilterCustom";
        public static string Key_HideOnLowZoom => "Sunglasses.HideOnLowZoom ";

        public static bool DrawNames
        {
            get
            {
                return Grasshopper.Instances.Settings.GetValue(Key_DrawNames, true);
            }
            set
            {
                Grasshopper.Instances.Settings.SetValue(Key_DrawNames, value);
            }
        }
        public static float FontSize
        {
            get
            {
                return (float)Grasshopper.Instances.Settings.GetValue(Key_FontSize, 8.0);
            }
            set
            {
                Grasshopper.Instances.Settings.SetValue(Key_FontSize, (double)value);
            }
        }
        public static bool HideOnLowZoom
        {
            get
            {
                return Grasshopper.Instances.Settings.GetValue(Key_HideOnLowZoom, true);
            }
            set
            {
                Grasshopper.Instances.Settings.SetValue(Key_HideOnLowZoom, value);
            }
        }
        public static bool DisplayNicknames
        {
            get
            {
                return Grasshopper.Instances.Settings.GetValue(Key_DisplayNicknames, false);
            }
            set
            {
                Grasshopper.Instances.Settings.SetValue(Key_DisplayNicknames, value);
            }
        }
        public static bool FilterComponents
        {
            get
            {
                return Grasshopper.Instances.Settings.GetValue(Key_FilterComponents, true);
            }
            set
            {
                Grasshopper.Instances.Settings.SetValue(Key_FilterComponents, value);
            }
        }
        public static bool FilterParameters
        {
            get
            {
                return Grasshopper.Instances.Settings.GetValue(Key_FilterParameters, true);
            }
            set
            {
                Grasshopper.Instances.Settings.SetValue(Key_FilterParameters, value);
            }
        }
        public static bool FilterSpecial
        {
            get
            {
                return Grasshopper.Instances.Settings.GetValue(Key_FilterSpecial, true);
            }
            set
            {
                Grasshopper.Instances.Settings.SetValue(Key_FilterSpecial, value);
            }
        }
        public static bool FilterGraphic
        {
            get
            {
                return Grasshopper.Instances.Settings.GetValue(Key_FilterGraphic, false);
            }
            set
            {
                Grasshopper.Instances.Settings.SetValue(Key_FilterGraphic, value);
            }
        }
        public static string FilterCustom
        {
            get
            {
                return Grasshopper.Instances.Settings.GetValue(Key_FilterCustom, string.Empty);
            }
            set
            {
                Grasshopper.Instances.Settings.SetValue(Key_FilterCustom, value);
            }
        }
         
        public static void PaintNames(Graphics G, IEnumerable<IGH_DocumentObject> objects)
        {
            if (HideOnLowZoom && GH_Canvas.ZoomFadeLow < 5)
                return;
            var alpha = HideOnLowZoom ? GH_Canvas.ZoomFadeLow : 255;
            var size = FontSize;
            var infl = size * 25;
            var hght = size * 2f;
            var nicknames = DisplayNicknames;
            G.TextRenderingHint = System.Drawing.Text.TextRenderingHint.AntiAlias;
            StringFormat sf = new StringFormat() { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center };
            Font font = GH_FontServer.NewFont(GH_FontServer.Standard, size / Grasshopper.GUI.GH_GraphicsUtil.UiScale, FontStyle.Italic);

            foreach (IGH_DocumentObject obj in objects)
            { 
                if (obj.Attributes == null)
                    obj.CreateAttributes();

                RectangleF box = obj.Attributes.Bounds;
                box.Inflate(infl, 0);
                box.Height = hght;
                box.Y -= box.Height + 2;

                GH_Palette palette = obj is IGH_ActiveObject a ? GH_CapsuleRenderEngine.GetImpliedPalette(a) : GH_Palette.Normal;
                GH_PaletteStyle style = GH_CapsuleRenderEngine.GetImpliedStyle(palette, obj.Attributes);
                using (Brush brh = new SolidBrush(Color.FromArgb(alpha, style.Edge)))
                {
                    G.DrawString(nicknames ? obj.NickName : obj.Name, font, brh, box, sf);
                }
            }
            sf.Dispose();
            font.Dispose();
        }
       
    }
}
