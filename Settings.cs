using Grasshopper.Kernel;
using System.Drawing; 

namespace Sunglasses
{
    public static class Settings 
    { 
        public static string Key_DrawNames => "Sunglasses.DrawNames";
        public static string Key_Font => "Sunglasses.Font";
        public static string Key_DisplayNicknames => "Sunglasses.DisplayNicknames";
        public static string Key_DisplayRichedCapsules => "Sunglasses.DisplayRichedAttributes";
        public static string Key_FilterComponents => "Sunglasses.FilterComponents";
        public static string Key_FilterParameters => "Sunglasses.FilterParameters";
        public static string Key_FilterSpecial => "Sunglasses.FilterSpecial";
        public static string Key_FilterGraphic => "Sunglasses.FilterGraphic";
        public static string Key_FilterCustom => "Sunglasses.FilterCustom";
        public static string Key_HideOnLowZoom => "Sunglasses.HideOnLowZoom ";

        #region Fonts
        private static Font _font;
        private static Font _fontCapsuleInfoName;
        private static Font _fontCapsuleDescription;
        private static Font _fontCapsuleInstanceDescription;
        private static Font _fontCapsuleParameterName;
        private static Font _fontCapsuleParameterData;

        public static Font Font
        {
            get
            {
                if (_font == null)
                {
                    _font = GH_FontServer.StringToFont(Grasshopper.Instances.Settings.GetValue(Key_Font, GH_FontServer.FontToString(GH_FontServer.StandardItalic)));
                }
                return _font;
            }
            set
            {
                if (value == null)
                    return;
                _font = value;
                Grasshopper.Instances.Settings.SetValue(Key_Font, GH_FontServer.FontToString(value));
            }
        }
        internal static Font FontCapsuleInfoName
        {
            get
            {
                if (_fontCapsuleInfoName == null)
                    _fontCapsuleInfoName = new Font(Font.FontFamily, Drawing.ScaleSize(2f), FontStyle.Bold);
                return _fontCapsuleInfoName;
            }

        }
        internal static Font FontCapsuleDescription
        {
            get
            {
                if (_fontCapsuleDescription == null)
                    _fontCapsuleDescription = new Font(Font.FontFamily, Drawing.ScaleSize(1.2f), FontStyle.Italic);
                return _fontCapsuleDescription;
            }

        }
        internal static Font FontCapsuleInstanceDescription
        {
            get
            {
                if (_fontCapsuleInstanceDescription == null)
                    _fontCapsuleInstanceDescription = new Font(Font.FontFamily, Drawing.ScaleSize(0.85f), FontStyle.Italic);
                return _fontCapsuleInstanceDescription;
            }

        }
        internal static Font FontCapsuleParameterName
        {
            get
            {
                if (_fontCapsuleParameterName == null)
                    _fontCapsuleParameterName = new Font(Font.FontFamily, Drawing.ScaleSize(2f), FontStyle.Regular);
                return _fontCapsuleParameterName;
            }

        }
        internal static Font FontCapsuleParameterData
        {
            get
            {
                if (_fontCapsuleParameterData == null)
                    _fontCapsuleParameterData = new Font(Font.FontFamily, Drawing.ScaleSize(0.75f), FontStyle.Italic);
                return _fontCapsuleParameterData;
            }

        }
        #endregion

        public static bool DisplayNames
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
        public static bool DisplayRichedCapsules
        {
            get
            {
                return Grasshopper.Instances.Settings.GetValue(Key_DisplayRichedCapsules, true);
            }
            set
            {
                Grasshopper.Instances.Settings.SetValue(Key_DisplayRichedCapsules, value);
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

        public static void ResetFonts() 
        {
            _font = null;
            _fontCapsuleInfoName = null;
            _fontCapsuleDescription = null;
            _fontCapsuleInstanceDescription = null;
            _fontCapsuleParameterName = null;
            _fontCapsuleParameterData = null;
        }
        public static void Dispose()
        {
            if (_font != null)
                _font.Dispose();
            if (_fontCapsuleInfoName != null)
                _fontCapsuleInfoName.Dispose();
            if (_fontCapsuleDescription != null)
                _fontCapsuleDescription.Dispose();
            if (_fontCapsuleInstanceDescription != null)
                _fontCapsuleInstanceDescription.Dispose();
            if (_fontCapsuleParameterName != null)
                _fontCapsuleParameterName.Dispose();
        }
    }

}
