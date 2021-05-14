using Grasshopper.Kernel;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;

namespace Sunglasses
{
    public static class Settings 
    { 
        public static string Key_DrawNames => "Sunglasses.DrawNames";
        public static string Key_Font => "Sunglasses.Font";
        public static string Key_DisplayNicknames => "Sunglasses.DisplayNicknames";
        public static string Key_DisplayCustomNicknames => "Sunglasses.DisplayCustomNicknames";
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

        // Cache frequently-used values to improve performance
        // ??= isn't used to preserve compatibility with C# 7.3

        private static bool? m_DisplayNames;
        private static bool? m_DisplayNicknames;
        private static bool? m_DisplayCustomNicknames;
        private static bool? m_DisplayRichedCapsules;
        private static bool? m_HideOnLowZoom;
        private static bool? m_FilterComponents;
        private static bool? m_FilterParameters;
        private static bool? m_FilterSpecial;
        private static bool? m_FilterGraphic;
        private static string m_FilterCustom;

        private static HashSet<string> _customFilters;
        public static bool DisplayNames
        {
            get
            {
                return m_DisplayNames ?? 
                    (m_DisplayNames = 
                    Grasshopper.Instances.Settings.GetValue(Key_DrawNames, true)).Value;
            }
            set
            {
                m_DisplayNames = value;
                Grasshopper.Instances.Settings.SetValue(Key_DrawNames, value);
            }
        }
        public static bool DisplayNicknames
        {
            get
            {
                return m_DisplayNicknames ??
                    (m_DisplayNicknames =
                    Grasshopper.Instances.Settings.GetValue(Key_DisplayNicknames, false)).Value;
            }
            set
            {
                m_DisplayNicknames = value;
                Grasshopper.Instances.Settings.SetValue(Key_DisplayNicknames, value);
            }
        }
        public static bool DisplayCustomNicknames
        {
            get
            {
                return m_DisplayCustomNicknames ??
                       (m_DisplayCustomNicknames =
                       Grasshopper.Instances.Settings.GetValue(Key_DisplayCustomNicknames, false)).Value;
            }
            set
            {
                m_DisplayCustomNicknames = value;
                Grasshopper.Instances.Settings.SetValue(Key_DisplayCustomNicknames, value);
            }
        }
        public static bool DisplayRichedCapsules
        {
            get
            {
                return m_DisplayRichedCapsules ??
                    (m_DisplayRichedCapsules =
                    Grasshopper.Instances.Settings.GetValue(Key_DisplayRichedCapsules, true)).Value;
            }
            set
            {
                m_DisplayRichedCapsules = value;
                Grasshopper.Instances.Settings.SetValue(Key_DisplayRichedCapsules, value);
            }
        }
        public static bool HideOnLowZoom
        {
            get
            {
                return m_HideOnLowZoom ??
                    (m_HideOnLowZoom =
                    Grasshopper.Instances.Settings.GetValue(Key_HideOnLowZoom, true)).Value;
            }
            set
            {
                m_HideOnLowZoom = value;
                Grasshopper.Instances.Settings.SetValue(Key_HideOnLowZoom, value);
            }
        }
        public static bool FilterComponents
        {
            get
            {
                return m_FilterComponents ??
                    (m_FilterComponents =
                    Grasshopper.Instances.Settings.GetValue(Key_FilterComponents, true)).Value;
            }
            set
            {
                m_FilterComponents = value;
                Grasshopper.Instances.Settings.SetValue(Key_FilterComponents, value);
            }
        }
        public static bool FilterParameters
        {
            get
            {
                return m_FilterParameters ??
                    (m_FilterParameters =
                    Grasshopper.Instances.Settings.GetValue(Key_FilterParameters, true)).Value;
            }
            set
            {
                m_FilterParameters = value;
                Grasshopper.Instances.Settings.SetValue(Key_FilterParameters, value);
            }
        }
        public static bool FilterSpecial
        {
            get
            {
                return m_FilterSpecial ??
                    (m_FilterSpecial =
                    Grasshopper.Instances.Settings.GetValue(Key_FilterSpecial, true)).Value;
            }
            set
            {
                m_FilterSpecial = value;
                Grasshopper.Instances.Settings.SetValue(Key_FilterSpecial, value);
            }
        }
        public static bool FilterGraphic
        {
            get
            {
                return m_FilterGraphic ??
                    (m_FilterGraphic =
                    Grasshopper.Instances.Settings.GetValue(Key_FilterGraphic, false)).Value;
            }
            set
            {
                m_FilterGraphic = value;
                Grasshopper.Instances.Settings.SetValue(Key_FilterGraphic, value);
            }
        }
        public static string FilterCustom
        {
            get
            {
                return m_FilterCustom ??
                    (m_FilterCustom =
                    Grasshopper.Instances.Settings.GetValue(Key_FilterCustom, string.Empty));
            }
            set
            {
                m_FilterCustom = value;
                Grasshopper.Instances.Settings.SetValue(Key_FilterCustom, value);
                UpdateFilterHashset();
            }
        }

        public static bool IsFilterCustomEnabled => !string.IsNullOrEmpty(FilterCustom);

        public static bool ShouldExcludeObject(string objectName)
        {
            if (_customFilters == null)
                UpdateFilterHashset();

            return _customFilters.Contains(objectName);
        }

        private static void UpdateFilterHashset()
        {
            var enumerable = FilterCustom.Split(',').Select(t => t.Trim());
            if (_customFilters == null)
            {
                _customFilters = new HashSet<string>(enumerable);
            }
            else
            {
                _customFilters.Clear();
                _customFilters.UnionWith(enumerable);
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
            // For better readibility

            _font?.Dispose();
            _fontCapsuleInfoName?.Dispose();
            _fontCapsuleDescription?.Dispose();
            _fontCapsuleInstanceDescription?.Dispose();
            _fontCapsuleParameterName?.Dispose();
        }
    }

}
