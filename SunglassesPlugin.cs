using System;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using Grasshopper.GUI;
using Grasshopper.Kernel;

namespace Sunglasses
{
    public class SunglassesPriority : GH_AssemblyPriority
    {
        public override GH_LoadingInstruction PriorityLoad()
        {
            Grasshopper.Instances.CanvasCreated += Instances_CanvasCreated;
            return GH_LoadingInstruction.Proceed;
        }

        private void Instances_CanvasCreated(Grasshopper.GUI.Canvas.GH_Canvas canvas)
        {
            Grasshopper.Instances.CanvasCreated -= Instances_CanvasCreated;

            var editor = Grasshopper.Instances.DocumentEditor;
            if (editor == null)
                return;
            var menu = editor.MainMenuStrip.Items.Find("mnuDisplay", false);
            if (menu == null)
            {
                Rhino.RhinoApp.WriteLine("Sunglasses cannot find Display menu.");
                return;
            } 
            ((ToolStripMenuItem)menu[0]).DropDownItems.Insert(3, new SunglassesMenuItem());

        }
    }

    public class SunglassesMenuItem : ToolStripMenuItem
    {
        public SunglassesMenuItem() 
            : base("Draw Names At Top", Properties.Resources.sunglasses, ClickHandler)
        {
            Name = "SunglassesMenuItem";
            Size = new Size(200, 30);
            ToolTipText = "Draw the name above the component, so that it can be used in icon mode.";
            Checked = Utils.DrawNames;
            var slider = new GH_DigitScroller
            {
                MinimumValue = 3,
                MaximumValue = 256,
                DecimalPlaces = 1,
                Value = (decimal)Utils.FontSize,
                Size = new Size(200, 24)
            };
            slider.ValueChanged += Slider_ValueChanged; 
            GH_DocumentObject.Menu_AppendCustomItem(DropDown, slider);

            GH_DocumentObject.Menu_AppendItem(DropDown, "Display nicknames",
             DisplayNicknamesHandler, true, Utils.DisplayNicknames)
             .ToolTipText = "Draw the nickname instead of the name of the objects.";

            GH_DocumentObject.Menu_AppendItem(DropDown, "Hide on low zoom",
      HideOnLowZoomHandler, true, Utils.HideOnLowZoom)
       .ToolTipText = "If checked, the name disappears when the canvas zoom is very low.";

            var filter = GH_DocumentObject.Menu_AppendItem(DropDown, "Filter objects");
            filter.ToolTipText = "Select in which objects to draw the name.";
            GH_DocumentObject.Menu_AppendItem(filter.DropDown, "Components", FilterComponentHandler, true, Utils.FilterComponents)
                .ToolTipText = "If checked, draw the name of components.";
            GH_DocumentObject.Menu_AppendItem(filter.DropDown, "Parameters", FilterParameterHandler, true, Utils.FilterParameters)
                .ToolTipText = "If checked, draw the name of parameters.";
            GH_DocumentObject.Menu_AppendItem(filter.DropDown, "Special", FilterSpecialHandler, true, Utils.FilterSpecial)
                .ToolTipText = "If checked, draw the name of special objects, like Number Slider, Panel, Gradient...";
            GH_DocumentObject.Menu_AppendItem(filter.DropDown, "Graphics", FilterGraphicHandler, true, Utils.FilterGraphic)
    .ToolTipText = "If checked, draw the name of graphic objects, like Group, Scribble, Jump...";
            var customFilter = GH_DocumentObject.Menu_AppendItem(filter.DropDown, "Exclusions");
            customFilter.ToolTipText = "Insert the names of the objects, separated by commas, that you want to exclude.";
            //GH_DocumentObject.Menu_AppendTextItem(customFilter.DropDown, Utils.FilterCustom, FilterCustomKeyDownHandler, FilterCustomTextChangedHandler, true); 
            var textBox = new ToolStripTextBox()
            {
                Size = new Size(200, 24),
                Text = Utils.FilterCustom,
                BorderStyle = BorderStyle.FixedSingle
        };
            textBox.TextChanged += TextBox_TextChanged;
            customFilter.DropDownItems.Add(textBox);
         
        
        }

        private void TextBox_TextChanged(object sender, EventArgs e)
        {
            var item = (ToolStripTextBox)sender;
            if (item == null)
                return;
            if (IsValidFilterCustom(item.Text))
            {
                item.ForeColor = Color.Black;
                 Utils.FilterCustom = item.Text;
                Grasshopper.Instances.ActiveCanvas?.Refresh();
            }
            else
            {
                item.ForeColor = Color.IndianRed;
            }
        }

        private void FilterComponentHandler(object sender, EventArgs e)
        {
            ((ToolStripMenuItem)sender).Checked = Utils.FilterComponents = !Utils.FilterComponents;
            Grasshopper.Instances.ActiveCanvas?.Refresh();
        }
        private void FilterParameterHandler(object sender, EventArgs e)
        {
            ((ToolStripMenuItem)sender).Checked = Utils.FilterParameters = !Utils.FilterParameters;
            Grasshopper.Instances.ActiveCanvas?.Refresh();
        }
        private void FilterGraphicHandler(object sender, EventArgs e)
        {
            ((ToolStripMenuItem)sender).Checked = Utils.FilterGraphic = !Utils.FilterGraphic;
            Grasshopper.Instances.ActiveCanvas?.Refresh();
        }
        private void FilterSpecialHandler(object sender, EventArgs e)
        {
            ((ToolStripMenuItem)sender).Checked = Utils.FilterSpecial = !Utils.FilterSpecial;
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
            ((ToolStripMenuItem)sender).Checked = Utils.DisplayNicknames = !Utils.DisplayNicknames;
            Grasshopper.Instances.ActiveCanvas?.Refresh();
        }
        private void HideOnLowZoomHandler(object sender, EventArgs e)
        {
            ((ToolStripMenuItem)sender).Checked = Utils.HideOnLowZoom = !Utils.HideOnLowZoom;
            Grasshopper.Instances.ActiveCanvas?.Refresh();
        }

        private void Slider_ValueChanged(object sender, Grasshopper.GUI.Base.GH_DigitScrollerEventArgs e)
        {
            Utils.FontSize = (float)e.Value;
            Grasshopper.Instances.ActiveCanvas?.Refresh();
        }

        private static void ClickHandler(object sender, EventArgs e)
        {
            var item = (SunglassesMenuItem)sender;
            item.Checked = !item.Checked;
        }

        protected override void OnCheckedChanged(EventArgs e)
        {
            base.OnCheckedChanged(e);
            Utils.DrawNames = Checked;

            var canvas = Grasshopper.Instances.ActiveCanvas;
            if (canvas == null)
                return;

            if (Checked)
            {
                canvas.CanvasPrePaintObjects -= Canvas_CanvasPrePaintObjects;
                canvas.CanvasPrePaintObjects += Canvas_CanvasPrePaintObjects;
            }
            else
            {
                canvas.CanvasPrePaintObjects -= Canvas_CanvasPrePaintObjects;
            }

            canvas.Refresh();
        }

        private bool ObjectFilter(IGH_DocumentObject obj)
        {
            var custom = Utils.FilterCustom;
            if (!string.IsNullOrEmpty(custom))
            {
                var split = custom.Split(',').Select(t => t.TrimStart().TrimEnd());
                if (split.Contains(obj.Name))
                    return false;
            }
            if (obj is Grasshopper.Kernel.Special.GH_Cluster)
                return Utils.FilterComponents;
            if (obj.GetType().Namespace == "Grasshopper.Kernel.Special")
                return Utils.FilterSpecial;
            if (obj is IGH_Component || obj.ComponentGuid == galapagosID)
                return Utils.FilterComponents;
            if (obj is IGH_Param)
                return Utils.FilterParameters;
            if(!(obj is IGH_ActiveObject))
            {
               return Utils.FilterGraphic; 
            }
            return false;
        }

        private void Canvas_CanvasPrePaintObjects(Grasshopper.GUI.Canvas.GH_Canvas sender)
        {
            if (!sender.IsDocument)
                return;
             
            Utils.PaintNames(sender.Graphics, sender.Document.Objects.Where(obj => ObjectFilter(obj)));
        }

        private Guid galapagosID = new Guid("E6DD2904-14BC-455b-8376-948BF2E3A7BC");
    }
     
    public class SunglassesInfo : GH_AssemblyInfo
    {
        public override string Name => "Sunglasses";
        public override Bitmap Icon => null;
        public override string Description => "Draw the name above the component, so that it can be used in icon mode.";
        public override Guid Id => new Guid("194607e9-d4d6-4e5a-836f-a65774231315");
        public override string AuthorName => "Daniel Gonzalez Abalde";
        public override string AuthorContact => "dga_3@hotmail.com"; 
        public override GH_LibraryLicense License => GH_LibraryLicense.opensource;
        public override string Version => "1.0.0";
    }
}
