using System;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using Grasshopper.GUI;
using Grasshopper.Kernel;

namespace Sunglasses
{

    public class SunglassesMenuItem : ToolStripMenuItem
    {
        public SunglassesMenuItem()
            : base("Sunglasses", Properties.Resources.sunglasses, ClickHandler)
        {
            Name = "SunglassesMenuItem";
            Size = new Size(200, 30);
            ToolTipText = "Draw the name above the component, so that it can be used in icon mode.";
            Checked = Settings.DisplayNames;
            //var slider = new GH_DigitScroller
            //{
            //    MinimumValue = 3,
            //    MaximumValue = 256,
            //    DecimalPlaces = 1,
            //    Value = (decimal)Settings.FontSize,
            //    Size = new Size(200, 24)
            //};
            //slider.ValueChanged += Slider_ValueChanged; 
            //GH_DocumentObject.Menu_AppendCustomItem(DropDown, slider);
            GH_DocumentObject.Menu_AppendItem(DropDown, "Select font", SelectFontHandler);

            GH_DocumentObject.Menu_AppendItem(DropDown, "Display nicknames",
             DisplayNicknamesHandler, true, Settings.DisplayNicknames)
             .ToolTipText = "Draw the nickname instead of the name of the objects.";

            GH_DocumentObject.Menu_AppendItem(DropDown, "Hide on low zoom",
      HideOnLowZoomHandler, true, Settings.HideOnLowZoom)
       .ToolTipText = "If checked, the name disappears when the canvas zoom is very low.";

            var filter = GH_DocumentObject.Menu_AppendItem(DropDown, "Filter objects");
            filter.ToolTipText = "Select in which objects to draw the name.";

            GH_DocumentObject.Menu_AppendItem(filter.DropDown, "Components", FilterComponentHandler, true, Settings.FilterComponents)
            .ToolTipText = "If checked, draw the name of components.";
            GH_DocumentObject.Menu_AppendItem(filter.DropDown, "Parameters", FilterParameterHandler, true, Settings.FilterParameters)
                .ToolTipText = "If checked, draw the name of parameters.";
            GH_DocumentObject.Menu_AppendItem(filter.DropDown, "Special", FilterSpecialHandler, true, Settings.FilterSpecial)
                .ToolTipText = "If checked, draw the name of special objects, like Number Slider, Panel, Gradient...";
            GH_DocumentObject.Menu_AppendItem(filter.DropDown, "Graphics", FilterGraphicHandler, true, Settings.FilterGraphic)
    .ToolTipText = "If checked, draw the name of graphic objects, like Group, Scribble, Jump...";
            var customFilter = GH_DocumentObject.Menu_AppendItem(filter.DropDown, "Exclusions");
            customFilter.ToolTipText = "Insert the names of the objects, separated by commas, that you want to exclude.";
            //GH_DocumentObject.Menu_AppendTextItem(customFilter.DropDown, Utils.FilterCustom, FilterCustomKeyDownHandler, FilterCustomTextChangedHandler, true); 
            var textBox = new ToolStripTextBox()
            {
                Size = new Size(200, 24),
                Text = Settings.FilterCustom,
                BorderStyle = BorderStyle.FixedSingle
            };
            textBox.TextChanged += TextBox_TextChanged;
            customFilter.DropDownItems.Add(textBox);

            GH_DocumentObject.Menu_AppendSeparator(DropDown);
            var riched = GH_DocumentObject.Menu_AppendItem(DropDown, "Riched capsules", DisplayRichedAttributesHandler, true, Settings.DisplayRichedCapsules);
            riched.ToolTipText = "Shows all the information within a component when zoomed in. ";

        }

        private void SelectFontHandler(object sender, EventArgs e)
        {
            var form = GH_FontPicker.CreateFontPickerWindow(Settings.Font);
            form.CreateControl();
            var picker = form.Controls.OfType<GH_FontPicker>().FirstOrDefault();
            if (picker == null)
                return;
            var panel = form.Controls.OfType<Panel>().FirstOrDefault();
            if (panel == null)
                return;

            var pickerType = typeof(GH_FontPicker);
            var sizeScroller = pickerType.GetField("_SizeScroller", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance).GetValue(picker) as GH_DigitScroller;
            var boldChecker = pickerType.GetField("_BoldCheck", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance).GetValue(picker) as CheckBox;
            var italicChecker = pickerType.GetField("_ItalicCheck", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance).GetValue(picker) as CheckBox;
            var fontScroller = pickerType.GetField("_FontList", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance).GetValue(picker) as GH_FontList;
            sizeScroller.ValueChanged += PreviewFontChangedHandler;
            boldChecker.CheckedChanged += PreviewFontChangedHandler;
            italicChecker.CheckedChanged += PreviewFontChangedHandler;
            fontScroller.MouseClick += PreviewFontChangedHandler;

            var defaultButton = new Button()
            {
                Text = "Default",
                Width = Grasshopper.Global_Proc.UiAdjust(80),
                Dock = DockStyle.Right,
                DialogResult = DialogResult.Yes
            };
            defaultButton.Click += DefaultFontHandler;
            panel.Controls.Add(defaultButton);

            var editor = Grasshopper.Instances.DocumentEditor;
            GH_WindowsFormUtil.CenterFormOnWindow(form, editor, true);
            var result = form.ShowDialog(editor);
            if (result == DialogResult.OK)
            {
                var font = form.Tag as Font;
                if (font != null)
                {
                    Settings.ResetFonts();
                    Settings.Font = font;
                }
            }
            else if (result == DialogResult.Yes)
            {
                Settings.ResetFonts();
                Settings.Font = GH_FontServer.StandardItalic;
            }
            Grasshopper.Instances.ActiveCanvas?.Refresh();
            sizeScroller.ValueChanged -= PreviewFontChangedHandler;
            boldChecker.CheckedChanged -= PreviewFontChangedHandler;
            italicChecker.CheckedChanged -= PreviewFontChangedHandler;
            fontScroller.MouseClick -= PreviewFontChangedHandler;
            defaultButton.Click -= DefaultFontHandler;

            void PreviewFontChangedHandler(object s, EventArgs args)
            {
                var font = picker.SelectedFont;
                if (font != null)
                {
                    var currentFont = Settings.Font;
                    Settings.Font = font;
                    Grasshopper.Instances.ActiveCanvas?.Refresh();
                    Settings.Font = currentFont;
                }
            }
            void DefaultFontHandler(object s, EventArgs args)
            {

            }
        }

        private void TextBox_TextChanged(object sender, EventArgs e)
        {
            var item = (ToolStripTextBox)sender;
            if (item == null)
                return;
            if (IsValidFilterCustom(item.Text))
            {
                item.ForeColor = Color.Black;
                Settings.FilterCustom = item.Text;
                Grasshopper.Instances.ActiveCanvas?.Refresh();
            }
            else
            {
                item.ForeColor = Color.IndianRed;
            }
        }

        private void FilterComponentHandler(object sender, EventArgs e)
        {
            ((ToolStripMenuItem)sender).Checked = Settings.FilterComponents = !Settings.FilterComponents;
            Grasshopper.Instances.ActiveCanvas?.Refresh();
        }
        private void FilterParameterHandler(object sender, EventArgs e)
        {
            ((ToolStripMenuItem)sender).Checked = Settings.FilterParameters = !Settings.FilterParameters;
            Grasshopper.Instances.ActiveCanvas?.Refresh();
        }
        private void FilterGraphicHandler(object sender, EventArgs e)
        {
            ((ToolStripMenuItem)sender).Checked = Settings.FilterGraphic = !Settings.FilterGraphic;
            Grasshopper.Instances.ActiveCanvas?.Refresh();
        }
        private void FilterSpecialHandler(object sender, EventArgs e)
        {
            ((ToolStripMenuItem)sender).Checked = Settings.FilterSpecial = !Settings.FilterSpecial;
            Grasshopper.Instances.ActiveCanvas?.Refresh();
        }

        private bool IsValidFilterCustom(string text)
        {
            if (string.IsNullOrEmpty(text))
                return true;

            var split = text.Split(',')
                .Select(t => t.TrimStart().TrimEnd())
                .Where(t => !string.IsNullOrWhiteSpace(t))
                .ToArray();

            return split.All(t => Grasshopper.Instances.ComponentServer.FindObjectByName(t, true, true) != null);
        }

        private void DisplayNicknamesHandler(object sender, EventArgs e)
        {
            ((ToolStripMenuItem)sender).Checked = Settings.DisplayNicknames = !Settings.DisplayNicknames;
            Grasshopper.Instances.ActiveCanvas?.Refresh();
        }
        private void DisplayRichedAttributesHandler(object sender, EventArgs e)
        {
            ((ToolStripMenuItem)sender).Checked = Settings.DisplayRichedCapsules = !Settings.DisplayRichedCapsules;
            Grasshopper.Instances.ActiveCanvas?.Refresh();
        }
        private void HideOnLowZoomHandler(object sender, EventArgs e)
        {
            ((ToolStripMenuItem)sender).Checked = Settings.HideOnLowZoom = !Settings.HideOnLowZoom;
            Grasshopper.Instances.ActiveCanvas?.Refresh();
        }

        //private void Slider_ValueChanged(object sender, Grasshopper.GUI.Base.GH_DigitScrollerEventArgs e)
        //{
        //    Settings.FontSize = (float)e.Value;
        //    Grasshopper.Instances.ActiveCanvas?.Refresh();
        //}

        private static void ClickHandler(object sender, EventArgs e)
        {
            var item = (SunglassesMenuItem)sender;
            item.Checked = !item.Checked;
        }

        private bool ObjectFilter(IGH_DocumentObject obj)
        {
            var custom = Settings.FilterCustom;
            if (!string.IsNullOrEmpty(custom))
            {
                var split = custom.Split(',').Select(t => t.TrimStart().TrimEnd());
                if (split.Contains(obj.Name))
                    return false;
            }
            if (obj is Grasshopper.Kernel.Special.GH_Cluster)
                return Settings.FilterComponents;
            if (obj.GetType().Namespace == "Grasshopper.Kernel.Special")
                return Settings.FilterSpecial;
            if (obj is IGH_Component || obj.ComponentGuid == galapagosID)
                return Settings.FilterComponents;
            if (obj is IGH_Param)
                return Settings.FilterParameters;
            if (!(obj is IGH_ActiveObject))
            {
                return Settings.FilterGraphic;
            }
            return false;
        }

        protected override void OnCheckedChanged(EventArgs e)
        {
            base.OnCheckedChanged(e);
            Settings.DisplayNames = Checked;

            var canvas = Grasshopper.Instances.ActiveCanvas;
            if (canvas == null)
                return;

            if (Checked)
            {
                canvas.CanvasPrePaintObjects -= Canvas_CanvasPrePaintObjects;
                canvas.CanvasPrePaintObjects += Canvas_CanvasPrePaintObjects;
                canvas.CanvasPostPaintObjects -= Canvas_CanvasPostPaintObjects;
                canvas.CanvasPostPaintObjects += Canvas_CanvasPostPaintObjects;
            }
            else
            {
                canvas.CanvasPrePaintObjects -= Canvas_CanvasPrePaintObjects;
                canvas.CanvasPostPaintObjects -= Canvas_CanvasPostPaintObjects;
            }

            canvas.Refresh();
        }

        private IGH_DocumentObject[] _visibleObjects;
        private IGH_DocumentObject[] _filteredObjects;

        private void Canvas_CanvasPrePaintObjects(Grasshopper.GUI.Canvas.GH_Canvas sender)
        {
            if (!sender.IsDocument || !Settings.DisplayNames)
                return;

            _visibleObjects = null;
            _filteredObjects = null;

            if (!Settings.DisplayNames && !Settings.DisplayRichedCapsules || !sender.IsDocument)
                return;
            _visibleObjects = Drawing.GetVisibleObjects(sender, sender.Document.Objects).ToArray();
            if (_visibleObjects == null || _visibleObjects.Length == 0)
                return;

            _filteredObjects = _visibleObjects.Where(obj => ObjectFilter(obj)).ToArray();

            if (_filteredObjects == null || _visibleObjects.Length == 0)
                return;
            Drawing.PaintNames(sender.Graphics, _filteredObjects);
        }
        private void Canvas_CanvasPostPaintObjects(Grasshopper.GUI.Canvas.GH_Canvas sender)
        {
            if (!Settings.DisplayRichedCapsules || !sender.IsDocument)
                return;
            if (_visibleObjects == null || _visibleObjects.Length == 0)
                return;
            Drawing.PaintRichedCapsules(sender, _visibleObjects);
        }

        private Guid galapagosID = new Guid("E6DD2904-14BC-455b-8376-948BF2E3A7BC");
    }

}
