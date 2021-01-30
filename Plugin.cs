using System;
using System.Drawing;
using System.Windows.Forms;
using Grasshopper.Kernel;

namespace Sunglasses
{
    public class SunglassesPriority : GH_AssemblyPriority
    {
        public override GH_LoadingInstruction PriorityLoad()
        {
            Grasshopper.Instances.CanvasCreated += Instances_CanvasCreated;
            Grasshopper.Instances.CanvasDestroyed += Instances_CanvasDestroyed;
            return GH_LoadingInstruction.Proceed;
        }

        private void Instances_CanvasCreated(Grasshopper.GUI.Canvas.GH_Canvas canvas)
        {
            Grasshopper.Instances.CanvasCreated -= Instances_CanvasCreated;

            var editor = Grasshopper.Instances.DocumentEditor;
            if (editor == null)
                return;
            var menu = editor.MainMenuStrip.Items.Find("mnuDisplay", false);
            if (menu.Length==0)
            {
                Rhino.RhinoApp.WriteLine("Sunglasses cannot find Display menu.");
                return;
            } 
            ((ToolStripMenuItem)menu[0]).DropDownItems.Insert(3, new SunglassesMenuItem());

        }

        private void Instances_CanvasDestroyed(Grasshopper.GUI.Canvas.GH_Canvas canvas)
        {
            Settings.Dispose();
        }

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
        public override string Version => "1.1.0";
    }
}
