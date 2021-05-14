using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using Grasshopper.GUI;
using Grasshopper.Kernel;
using Grasshopper.Kernel.Special;

namespace Sunglasses
{

    public class SunglassesMenuItem : ToolStripMenuItem
    {
        private ToolStripMenuItem _menuNicknames;
        private ToolStripMenuItem _menuCustomNicknames;
        private ToolStripMenuItem _menuFilter;

        public SunglassesMenuItem()
            : base("Sunglasses", Properties.Resources.sunglasses, ClickHandler)
        {
            Name = "SunglassesMenuItem";
            Size = new Size(200, 30);
            ToolTipText = "Draw the name above the component, so that it can be used in icon mode.";
            Checked = Settings.DisplayNames;

            if (Rhino.Runtime.HostUtils.RunningOnWindows)
            {
                GH_DocumentObject.Menu_AppendItem(DropDown, "Select font", SelectFontHandler);
            }
            else
            {
                var slider = new Grasshopper.GUI.GH_DigitScroller
                {
                    MinimumValue = 3,
                    MaximumValue = 256,
                    DecimalPlaces = 1,
                    Value = (decimal)Settings.Font.Size,
                    Size = new Size(200, 24)
                };
                slider.ValueChanged += Slider_ValueChanged;
                GH_DocumentObject.Menu_AppendCustomItem(DropDown, slider);
            }

            _menuNicknames = GH_DocumentObject.Menu_AppendItem(DropDown, "Display nicknames",
             DisplayNicknamesHandler, true, Settings.DisplayNicknames);
            _menuNicknames.ToolTipText = "Draw the nickname instead of the name of the objects.";
            _menuCustomNicknames = GH_DocumentObject.Menu_AppendItem(DropDown, "Display only custom nicknames",
          DisplayCustomNicknamesHandler, true, Settings.DisplayCustomNicknames);
            _menuCustomNicknames.ToolTipText = "Draw just the user defined nicknames.";

            _menuFilter = GH_DocumentObject.Menu_AppendItem(DropDown, "Filter objects");
            _menuFilter.ToolTipText = "Select in which objects to draw the name.";

            GH_DocumentObject.Menu_AppendItem(_menuFilter.DropDown, "Components", FilterComponentHandler, true, Settings.FilterComponents)
            .ToolTipText = "If checked, draw the name of components.";
            GH_DocumentObject.Menu_AppendItem(_menuFilter.DropDown, "Parameters", FilterParameterHandler, true, Settings.FilterParameters)
                .ToolTipText = "If checked, draw the name of parameters.";
            GH_DocumentObject.Menu_AppendItem(_menuFilter.DropDown, "Special", FilterSpecialHandler, true, Settings.FilterSpecial)
                .ToolTipText = "If checked, draw the name of special objects, like Number Slider, Panel, Gradient...";
            GH_DocumentObject.Menu_AppendItem(_menuFilter.DropDown, "Graphics", FilterGraphicHandler, true, Settings.FilterGraphic)
    .ToolTipText = "If checked, draw the name of graphic objects, like Group, Scribble, Jump...";
            var customFilter = GH_DocumentObject.Menu_AppendItem(_menuFilter.DropDown, "Exclusions");
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

            GH_DocumentObject.Menu_AppendItem(DropDown, "Hide on low zoom",
      HideOnLowZoomHandler, true, Settings.HideOnLowZoom)
       .ToolTipText = "If checked, the name disappears when the canvas zoom is very low.";

            UpdateCustomNicknameMenuEntryState();
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
            var sizeScroller = pickerType.GetField("_SizeScroller", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance).GetValue(picker) as Grasshopper.GUI.GH_DigitScroller;
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

        private void UpdateCustomNicknameMenuEntryState()
        {
            if (!Settings.DisplayNicknames)
            {
                // Set Checked to false doesn't automatically provoke the corresponding event.
                // Set the setting yourself.

                _menuCustomNicknames.Checked = false;
                Settings.DisplayCustomNicknames = false;
            }

            _menuCustomNicknames.Enabled = Settings.DisplayNicknames;
        }
        private void DisplayNicknamesHandler(object sender, EventArgs e)
        {
            ((ToolStripMenuItem)sender).Checked = Settings.DisplayNicknames = !Settings.DisplayNicknames;
            UpdateCustomNicknameMenuEntryState();

            Grasshopper.Instances.ActiveCanvas?.Refresh();
        }
        private void DisplayCustomNicknamesHandler(object sender, EventArgs e)
        {
            ((ToolStripMenuItem)sender).Checked = Settings.DisplayCustomNicknames = !Settings.DisplayCustomNicknames;      
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

        private void Slider_ValueChanged(object sender, Grasshopper.GUI.Base.GH_DigitScrollerEventArgs e)
        {
            Settings.Font = new Font(Settings.Font.FontFamily, (float)e.Value);
            Grasshopper.Instances.ActiveCanvas?.Refresh();
        }

        private static void ClickHandler(object sender, EventArgs e)
        {
            var item = (SunglassesMenuItem)sender;
            item.Checked = !item.Checked;
        }

        const string NameOfClusterComponent = "Cluster";
        private bool ObjectFilter(IGH_DocumentObject obj)
        {
            // Hide nickname for panels
            if (Settings.DisplayNicknames || Settings.DisplayCustomNicknames)
            {
                // Hide group's nickname for now

                if (obj is GH_Panel || obj is GH_Group || obj is GH_NumberSlider)
                    return false;

                // TODO: Draw Group's nickname at the largest font size
                // in the group's container box to be able to read it when the zoom is too low.
            }

            // Move filter logic ahead to enable filter on custom nicknames.

            // Do not split the string in every paint call. Cache them in Settings.
            // String manipulation & IEnumerable.Contains are time-costly.
            if (Settings.IsFilterCustomEnabled &&
                Settings.ShouldExcludeObject(obj.Name))
                return false;

            if (!Settings.FilterComponents &&
                (obj is GH_Cluster || obj is IGH_Component || obj.ComponentGuid == galapagosID))
                return false;

            if (!Settings.FilterSpecial &&
                IsSpecialObject(obj))
                return false;

            if (!Settings.FilterParameters &&
                obj is IGH_Param)
                return false;

            if (!Settings.FilterGraphic &&
                !(obj is IGH_ActiveObject))
                return false;

            if (Settings.DisplayCustomNicknames)
            {
                var code = string.Join(".", obj.Category, obj.SubCategory, obj.Name);

                // Reduce string manipulation to improve performance
                var id = obj.ComponentGuid;

                if (!_nicknamesCache.TryGetValue(code, out var defNick))
                {
                    IGH_ObjectProxy proxy = null;
                    if (id == clusterID || id == vbID || id == csID || id == pyID || id == Guid.Empty)
                    {
                        proxy = Grasshopper.Instances.ComponentServer.FindObjectByName(obj.Name, true, true);
                    }
                    else
                    {
                        proxy = Grasshopper.Instances.ComponentServer.EmitObjectProxy(id);
                    }

                    if (proxy != null)
                    {
                        defNick = proxy.Desc.NickName;
                        _nicknamesCache.Add(code, defNick);
                    }
                    else
                    {
                        if (obj is GH_Cluster)
                        {
                            // A newly-created cluster, or a cluster which is not an existing user object.
                            // You cannot really know its original nickname
                            // Show the nickname if it's not 'Cluster' - take it as newly-created

                            return obj.NickName != NameOfClusterComponent;
                        }

                        Rhino.RhinoApp.WriteLine(obj.Name);
                        return true;
                    }
                }

                return !defNick.Equals(obj.NickName, StringComparison.OrdinalIgnoreCase);
            }

            return true;
        }

        // This method is created to cache results and improve performance.
        private static Dictionary<Guid, bool> _specialObjectGuids = new Dictionary<Guid, bool>();
        private static bool IsSpecialObject(IGH_DocumentObject obj)
        {
            // Cluster is regarded as a component, although it lies in the 'Special' namespace.
            if (obj is GH_Cluster)
                return false;

            var guid = obj.ComponentGuid;
            if (_specialObjectGuids.TryGetValue(guid, out var result)) return result;

            return _specialObjectGuids[guid] = obj.GetType().Namespace == "Grasshopper.Kernel.Special";
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

        private SortedDictionary<string, string> _nicknamesCache = new SortedDictionary<string, string>();

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
        private Guid clusterID = new Guid("f31d8d7a-7536-4ac8-9c96-fde6ecda4d0a");
        private Guid vbID = new Guid("079bd9bd-54a0-41d4-98af-db999015f63d");
        private Guid csID = new Guid("a9a8ebd2-fff5-4c44-a8f5-739736d129ba");
        private Guid pyID = new Guid("410755b1-224a-4c1e-a407-bf32fb45ea7e");
 

    }

}
