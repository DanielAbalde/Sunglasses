using System;
using Grasshopper.GUI.Canvas;
using Grasshopper.Kernel;
using Grasshopper.Kernel.Attributes;
using System.Collections.Generic;
using System.Drawing;

namespace Sunglasses
{

    public static class Drawing
    {
        #region Fields
        private static float _zoomFactor;
        private static Grasshopper.GUI.GH_FadeAnimation _fade;
        #endregion

        #region Properties 
        public static float ZoomFactor
        {
            get
            {
                return _zoomFactor;
            }
            internal set
            {
                _zoomFactor = (System.Math.Min(ZoomObjectsDisplayMax, System.Math.Max(ZoomObjectsDisplayMin, value)) - ZoomObjectsDisplayMin) / (ZoomObjectsDisplayMax - ZoomObjectsDisplayMin);
            }
        }
        public static Grasshopper.GUI.GH_FadeAnimation Fade
        {
            get
            {
                if (_fade == null)
                    _fade = new Grasshopper.GUI.GH_FadeAnimation(GH_Viewport.ZoomDefault * ZoomObjectsDisplayMin);
                return _fade;
            }
        }
        public static float BoxBorderWidth
        {
            get
            {
                return 0.1f / Grasshopper.GUI.GH_GraphicsUtil.UiScale;
            }
        }
        public static float ZoomObjectsDisplayMin { get { return 6f; } }
        public static float ZoomObjectsDisplayMax { get { return 10f; } }
        #endregion

        public static Color FadeColor(Color color)
        {
            return Color.FromArgb(Fade.FadeAlpha, color);
        }
        public static float ScaleSize(float size)
        {
            return size / Grasshopper.GUI.GH_GraphicsUtil.UiScale;
        }
        public static float Remap(float V, float A, float B, float C, float D)
        {
            return (V - A) / (B - A) * (D - C) + C;
        }
        public static RectangleF Snap(RectangleF rec, float snapping)
        {
            return new RectangleF(
                rec.X - rec.X % snapping,
                rec.Y - rec.Y % snapping,
                rec.Width - rec.Width % snapping,
                rec.Height - rec.Height % snapping
                );
        }

        public static void PaintNames(Graphics graphics, IEnumerable<IGH_DocumentObject> objects)
        {
            if (Settings.HideOnLowZoom && GH_Canvas.ZoomFadeLow < 5)
                return;
            try
            {
                var alpha = Settings.HideOnLowZoom ? GH_Canvas.ZoomFadeLow : 255;
                var size = Settings.Font.Size;
                var infl = size * 25;
                var hght = size * 2f;
                var nicknames = Settings.DisplayNicknames || Settings.DisplayCustomNicknames;
                graphics.TextRenderingHint = TextRenderingHint.AntiAlias;
                var sf = GH_TextRenderingConstants.CenterCenter;
                sf.FormatFlags |= StringFormatFlags.NoClip;
                graphics.TextRenderingHint = System.Drawing.Text.TextRenderingHint.AntiAlias;

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
                        graphics.DrawString(nicknames ? obj.NickName : obj.Name, Settings.Font, brh, box, sf);
                    }

                }
            }
            catch (Exception e)
            {
                Rhino.RhinoApp.WriteLine(e.ToString());
            }
        }

        public static void PaintRichedCapsules(GH_Canvas canvas, IEnumerable<IGH_DocumentObject> objects)
        {
            Fade.Evaluate(canvas, true);
            if (Fade.FadeAlpha < 1)
                return;
            ZoomFactor = canvas.Viewport.Zoom;
            if (ZoomFactor == 0)
                return;
            try
            {
                var graphics = canvas.Graphics;
                foreach (var obj in objects)
                {
                    if (!(obj is IGH_ActiveObject aObj))
                        continue;
                    if (obj.Attributes == null)
                        obj.CreateAttributes();
                    var objAtt = obj.Attributes;
                    var bounds = objAtt.Bounds;
                    if (!canvas.Viewport.IsVisible(ref bounds, 0))
                        continue;
                    RichedCapsule rObj = null;
                    if (objAtt is GH_ComponentAttributes cAtt)
                    {
                        rObj = new RichedCapsuleComponent(cAtt);
                    }
                    else if (objAtt is GH_FloatingParamAttributes pAtt && (aObj.IconDisplayMode == GH_IconDisplayMode.icon || aObj.IconDisplayMode == GH_IconDisplayMode.application && Grasshopper.CentralSettings.CanvasObjectIcons))
                    {
                        rObj = new RichedCapsuleParameter(pAtt);
                    }

                    if (rObj == null)
                        continue;

                    rObj.RenderRichedCapsule(graphics, canvas);

                    if (objAtt is GH_ComponentAttributes cAtt2)
                    {
                        typeof(GH_ComponentAttributes).GetMethod("RenderVariableParameterUI",
                            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
                        .Invoke(cAtt2, new object[] { canvas, graphics });
                    }
                }
            }
            catch (Exception e)
            {
                Rhino.RhinoApp.WriteLine(" :( Sunglasses exception painting riched capsules! try with another font ):");
                Rhino.RhinoApp.WriteLine(e.ToString());
            }
        }
        public static IEnumerable<IGH_DocumentObject> GetVisibleObjects(GH_Canvas canvas, IEnumerable<IGH_DocumentObject> objects)
        {
            foreach (var obj in objects)
            {
                RectangleF bnd = obj.Attributes.Bounds;
                if (canvas.Viewport.IsVisible(ref bnd, 20))
                {
                    yield return obj;
                    //if (obj is IGH_Component && obj.Attributes.GetType() == typeof(GH_ComponentAttributes))
                    //{
                    //    yield return new RichedDisplayComponent(obj as IGH_Component) { ShowParameterIcons = ShowComponentParameterIcons };
                    //}
                    //else if (obj is IGH_Param && Util.IsPersistantParameter(obj))
                    //{
                    //    yield return new RichedDisplayParameter(obj as IGH_Param) { };
                    //}
                }
            }
        }

        internal static List<string> GetDataDescription(Grasshopper.Kernel.Data.IGH_Structure data, Graphics graphics, Font font, SizeF limit, out float height)
        {
            var list = new List<string>();
            var totalHeight = 0f;
            var pathCount = data.PathCount;
            for (int i = 0; i < pathCount; i++)
            {
                var path = data.Paths[i]; ;
                if (!Add($"    {path}"))
                    break;
                var branch = data.get_Branch(path);
                int spacing = branch.Count.ToString().Length + 2;
                for (int j = 0; j < branch.Count; j++)
                {
                    if (branch[j] == null)
                    {
                        if (!Add($"{Indexing(j, spacing)}<null>"))
                            break;
                        continue;
                    }
                    string text = branch[j].ToString();
                    if (string.IsNullOrEmpty(text))
                    {
                        if (!Add($"{Indexing(j, spacing)}<empty>"))
                            break;
                    }
                    else
                    {
                        if (!Add($"{Indexing(j, spacing)}{text}"))
                            break;
                    }
                }

                if (i < pathCount - 1)
                    if (!Add(Environment.NewLine))
                        break;
            }
            height = totalHeight;
            return list;

            bool Add(string text)
            {
                list.Add(text);
                totalHeight += graphics.MeasureString(text, font, (int)Math.Ceiling(limit.Width)).Height;
                return totalHeight < limit.Height;
            }

            string Indexing(int i, int s)
            {
                var text = $"{i}. ";
                int num = s - text.Length;
                if (num > 0)
                {
                    text += new string(' ', num);
                }
                return text;
            }
        }
        internal static float PaintCapsuleParameterData(Graphics graphics, RectangleF bounds, IGH_Param param, GH_PaletteStyle style)
        {
            if (bounds.Width <= 2 || bounds.Height <= 1 || ZoomFactor < 0.5f)
                return 0f;
            var font = Settings.FontCapsuleParameterData;
            var dd = GetDataDescription(param.VolatileData, graphics, font, new SizeF(bounds.Width - graphics.MeasureString(param.VolatileDataCount.ToString(), font).Width, bounds.Height), out _);
            var count = dd.Count;

            var color = FadeColor(style.Edge);
            Color c0 = Color.FromArgb(20, color);
            Color c1 = Color.FromArgb(50, color);
            Color c2 = Color.FromArgb(100, color);

            var border = BoxBorderWidth;
            var border2 = border / 2f;
            var padding = ScaleSize(0.5f);
            var top = bounds.Y + padding / 2f;
            var bottom = bounds.Bottom - padding;
            var height = bottom - top;
            var left = bounds.X + padding;
            var right = bounds.Right - padding;
            var width = right - left;

            var fontTreeInfo = GH_FontServer.NewFont(font, font.Size * 0.9f);
            var fontItem = GH_FontServer.NewFont(font, FontStyle.Regular);
            var fontBranchCount = GH_FontServer.NewFont(font, FontStyle.Regular);

            var treeInfo = string.Empty;
            var empty = false;
            if (param.VolatileDataCount == 0)
            {
                treeInfo = "Empty parameter";
                empty = true;
            }
            else if (param.VolatileData.PathCount == 1)
            {
                int cnt = param.VolatileData.get_Branch(0).Count;
                if (cnt == 1)
                {
                    treeInfo = "Branch with 1 item";
                }
                else
                {
                    treeInfo = string.Format("Branch with {0} items", cnt.ToString());
                }
            }
            else
            {
                treeInfo = string.Format("Tree with {0} branches and {1} items", param.VolatileData.PathCount.ToString(), param.VolatileData.DataCount.ToString());
            }

            var cellHeight = graphics.MeasureString(treeInfo, font).Height;
            var cellHeight1 = cellHeight;

            var totalHeight = cellHeight;
            var currentY = top + cellHeight;
            if (currentY > bounds.Bottom)
            {
                return 0f;
            }

            PaintCapsuleBoxBackground(graphics, bounds, style);

            var textBrush = new SolidBrush(style.Text);

            if (bounds.Height < cellHeight * 3f)
            {
                graphics.DrawString(treeInfo, font, textBrush, bounds, Grasshopper.GUI.GH_TextRenderingConstants.CenterCenter);
            }
            else
            {
                if (empty)
                {
                    graphics.DrawString(treeInfo, font, textBrush, bounds, Grasshopper.GUI.GH_TextRenderingConstants.CenterCenter);
                }
                else
                {
                    graphics.DrawString(treeInfo, font, textBrush, new RectangleF(left, top, width, cellHeight), Grasshopper.GUI.GH_TextRenderingConstants.FarCenter);

                    int maxCP = 0;
                    for (int i = 0; i < param.VolatileData.PathCount; i++)
                    {
                        int cp = param.VolatileData.get_Branch(i).Count;
                        if (cp > maxCP)
                            maxCP = cp;
                    }
                    var widthIndex = graphics.MeasureString(maxCP.ToString(), font, (int)width).Width + ScaleSize(1f);
                    var widthItem = bounds.Width - widthIndex;
                    var column2X = bounds.X + widthIndex;
                    var column2width = bounds.Right - column2X;

                    var iter = 0;
                    var pathid = 0;
                    for (var i = 0; i < count; i++)
                    {
                        var s = dd[i].TrimStart();
                        var isB = s.StartsWith("{");

                        cellHeight = graphics.MeasureString(s, font, (int)width).Height;

                        totalHeight += cellHeight;

                        var nomore = false;
                        if (totalHeight >= height && !string.IsNullOrEmpty(s))
                        {
                            graphics.DrawString("...", font, textBrush, new RectangleF(column2X, bounds.Bottom - cellHeight1, widthItem, cellHeight1), Grasshopper.GUI.GH_TextRenderingConstants.CenterCenter);
                            nomore = true;
                        }

                        totalHeight -= cellHeight;
                        cellHeight = Math.Min(cellHeight, bounds.Bottom - currentY);
                        totalHeight += cellHeight;

                        if ((iter % 2 == 0 || isB) && !string.IsNullOrWhiteSpace(s.Replace("\r", "").Replace("\n", "")))
                        {
                            using (Brush brh = new SolidBrush(isB ? c1 : c0))
                                graphics.FillRectangle(brh, bounds.X, currentY, bounds.Width, cellHeight);
                            iter = 0;
                        }

                        if (isB)
                        {
                            using (Pen pen = new Pen(c2, border2) { StartCap = System.Drawing.Drawing2D.LineCap.Square, EndCap = System.Drawing.Drawing2D.LineCap.Square })
                                graphics.DrawLine(pen, bounds.X, currentY, bounds.Right, currentY);

                            if (!nomore)
                                graphics.DrawString(s, font, textBrush, new RectangleF(left, currentY, width, cellHeight), Grasshopper.GUI.GH_TextRenderingConstants.FarCenter);

                            float w1 = graphics.MeasureString(s, font).Width;
                            string listCount = $"N = {param.VolatileData.get_Branch(pathid).Count}";
                            float w2 = graphics.MeasureString(listCount, font).Width + padding;
                            if (w1 + w2 < width)
                                using (Brush brh = new SolidBrush(Color.FromArgb(200, style.Text)))
                                    graphics.DrawString(listCount, fontBranchCount, brh, new RectangleF(column2X + padding, currentY, column2width, cellHeight), Grasshopper.GUI.GH_TextRenderingConstants.NearCenter);
                            pathid++;
                        }
                        else
                        {
                            if (currentY + cellHeight < bounds.Bottom || i == count - 1)
                            {
                                string[] split = s.Split(new[] { '.' }, 2);
                                if (split.Length > 0)
                                    graphics.DrawString(split[0], font, textBrush, new RectangleF(bounds.X, currentY, widthIndex, cellHeight), Grasshopper.GUI.GH_TextRenderingConstants.CenterCenter);
                                if (split.Length > 1)
                                    graphics.DrawString(split[1], fontItem, textBrush, new RectangleF(column2X, currentY, column2width, cellHeight), Grasshopper.GUI.GH_TextRenderingConstants.CenterCenter);
                            }


                        }
                        if (nomore)
                            break;
                        currentY += cellHeight;
                        iter++;
                    }

                    using (Pen pen = new Pen(c2, border2) { StartCap = System.Drawing.Drawing2D.LineCap.Flat, EndCap = System.Drawing.Drawing2D.LineCap.Square })
                    {
                        graphics.DrawLine(pen, bounds.X + widthIndex, top + cellHeight1, bounds.X + widthIndex, bounds.Bottom);
                    }
                }



            }

            textBrush.Dispose();
            fontBranchCount.Dispose();
            fontItem.Dispose();
            fontTreeInfo.Dispose();

            PaintCapsuleBox(graphics, bounds, style);

            return bounds.Height;
        }

        internal static void PaintCapsuleBoxBackground(Graphics graphics, RectangleF bounds, GH_PaletteStyle style)
        {
            using (var brh = new SolidBrush(Color.FromArgb(15, FadeColor(Color.Black))))
                graphics.FillRectangle(brh, bounds);
        }
        internal static void PaintCapsuleBox(Graphics graphics, RectangleF bounds, GH_PaletteStyle style)
        {
            var shadowColor = Color.FromArgb(60, FadeColor(Color.Black));
            graphics.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
            ///Grasshopper.GUI.GH_GraphicsUtil.ShadowRectangle(graphics, bounds, 1, 60);
            var size = ZoomFactor >= 0.5f ? (1f - 0.5f * ZoomFactor) : 1f;

            float left = (float)Math.Floor(bounds.X);
            float top = (float)Math.Floor(bounds.Y);
            float right = (float)Math.Ceiling(bounds.Right);
            float bottom = (float)Math.Ceiling(bounds.Bottom);
            var rec = new RectangleF(left, top, right - left, bottom - top);

            //PaintShadow(graphics, new RectangleF(bounds.X, bounds.Y, bounds.Width, size), shadowColor, System.Drawing.Drawing2D.LinearGradientMode.Vertical, false);
            /////(graphics, new RectangleF(bounds.X, bounds.Bottom - size, bounds.Width, size), shadowColor, System.Drawing.Drawing2D.LinearGradientMode.Vertical,  true);
            // PaintShadow(graphics, new RectangleF(bounds.X, bounds.Y, size, bounds.Height), shadowColor, System.Drawing.Drawing2D.LinearGradientMode.Horizontal, false);
            //PaintShadow(graphics, new RectangleF(bounds.Right - size, bounds.Y, size, bounds.Height), shadowColor, System.Drawing.Drawing2D.LinearGradientMode.Horizontal, true);

            using (var brh = new System.Drawing.Drawing2D.LinearGradientBrush(rec, Color.FromArgb(0, shadowColor), shadowColor, System.Drawing.Drawing2D.LinearGradientMode.Horizontal))
            {
                brh.WrapMode = System.Drawing.Drawing2D.WrapMode.TileFlipXY;
                brh.Blend = new System.Drawing.Drawing2D.Blend()
                {
                    Positions = new float[] {
                        0f,
                        Remap(bounds.X, left, right, 0f, 1f),
                        Remap(bounds.X + size, left, right, 0f, 1f),
                        Remap(bounds.Right - size, left, right, 0f, 1f),
                        Remap(bounds.Right, left, right, 0f, 1f),
                        1f
                    },
                    Factors = new float[] { 1f, 1f, 0f, 0f, 1f, 1f }
                };
                graphics.FillRectangle(brh, bounds);
            }
            using (var brh = new System.Drawing.Drawing2D.LinearGradientBrush(rec, Color.FromArgb(0, shadowColor), shadowColor, System.Drawing.Drawing2D.LinearGradientMode.Vertical))
            {
                brh.WrapMode = System.Drawing.Drawing2D.WrapMode.TileFlipXY;
                brh.Blend = new System.Drawing.Drawing2D.Blend()
                {
                    Positions = new float[] {
                        0f,
                        Remap(bounds.Y, top, bottom, 0f, 1f),
                        Remap(bounds.Y + size, top, bottom, 0f, 1f),
                        Remap(bounds.Bottom - size, top, bottom, 0f, 1f),
                        Remap(bounds.Bottom  , top, bottom, 0f, 1f),
                        1f
                    },
                    Factors = new float[] { 1f, 1f, 0f, 0f, 1f, 1f }
                };
                graphics.FillRectangle(brh, bounds);
            }

            using (var pen = new Pen(FadeColor(style.Edge), BoxBorderWidth))
                graphics.DrawRectangle(pen, bounds.X, bounds.Y, bounds.Width, bounds.Height);
        }
        internal static void PaintRedBorders(Graphics graphics, RectangleF bounds)
        {
            using (var pen = new Pen(Color.Red, BoxBorderWidth))
                graphics.DrawRectangle(pen, bounds.X, bounds.Y, bounds.Width, bounds.Height);
        }

        public static Font AdjustFontHeight(Graphics graphics, Font font, string text, RectangleF bounds)
        {
            if (font.Size <= 0.1f)
                return font;
            var h = graphics.MeasureString(text, font, (int)Math.Ceiling(bounds.Width)).Height;
            if (h < bounds.Height)
                return font;
            var ratio = bounds.Height / h;
            var delta = Math.Max(0.1f, font.Size - font.Size * ratio);
            var newSize = Math.Max(0.1f, font.Size - 0.5f * delta);
            return AdjustFontHeight(graphics, new Font(font.Name, newSize, font.Style), text, bounds);

        }

        internal abstract class RichedCapsule
        {
            #region Fields 
            private float _padding;
            #endregion

            #region Properties
            public IGH_Attributes Attributes { get; }
            public IGH_ActiveObject Object { get; }
            public RectangleF Bounds { get; }
            public GH_Capsule Capsule { get; }
            public GH_PaletteStyle Style { get; }
            public float Padding
            {
                get
                {
                    if (_padding == 0f)
                        _padding = ScaleSize(1f);
                    return _padding;
                }
            }
            #endregion

            #region Constructors 
            public RichedCapsule(IGH_Attributes att)
            {
                Attributes = att;
                Object = att.GetTopLevel.DocObject as IGH_ActiveObject;
                Bounds = CreateBounds();
                var palette = GH_CapsuleRenderEngine.GetImpliedPalette(Object);
                Capsule = GH_Capsule.CreateCapsule(att.Bounds, palette);
                Capsule.SetJaggedEdges(!Attributes.HasInputGrip, !Attributes.HasOutputGrip);
                var defStyle = GH_CapsuleRenderEngine.GetImpliedStyle(palette, att);
                var alpha = (int)(255 * System.Math.Pow(Fade.FadeAlpha / 255.0, 0.1));
                Style = new GH_PaletteStyle(
                     defStyle.Fill,
                    Color.FromArgb(alpha, defStyle.Edge),
                     defStyle.Text);
            }
            #endregion

            #region Methods 
            protected virtual RectangleF CreateBounds()
            {
                return Attributes.Bounds;
            }
            public virtual Region GetClip()
            {
                return new Region(Bounds);
            }
            public abstract void Render(Graphics graphics, GH_Canvas canvas);
            public void RenderRichedCapsule(Graphics graphics, GH_Canvas canvas)
            {
                var clip = graphics.Clip;
                graphics.SetClip(GetClip(), System.Drawing.Drawing2D.CombineMode.Replace);

                Capsule.Render(graphics, Style);
                Render(graphics, canvas);

                graphics.SetClip(clip, System.Drawing.Drawing2D.CombineMode.Replace);

            }
            #endregion
        }
        internal class RichedCapsuleComponent : RichedCapsule
        {
            private bool _iconMode;
            internal RichedCapsuleComponentInfo Info { get; }
            internal List<RichedCapsuleComponentParameter> Parameters { get; }

            public RichedCapsuleComponent(GH_ComponentAttributes att) : base(att)
            {
                _iconMode = Grasshopper.CentralSettings.CanvasObjectIcons;
                if (_iconMode)
                    Info = new RichedCapsuleComponentInfo(att);
                Parameters = new List<RichedCapsuleComponentParameter>();
                foreach (var param in att.Owner.Params)
                {
                    if (param.Attributes is GH_LinkedParamAttributes pAtt)
                    {
                        Parameters.Add(new RichedCapsuleComponentParameter(pAtt));
                    }
                }
            }

            public override void Render(Graphics graphics, GH_Canvas canvas)
            {
                ///Rhino.RhinoApp.WriteLine();
                if (_iconMode)
                {
                    //var start = DateTime.Now;
                    Info.Render(graphics, canvas);
                    //var time = (DateTime.Now - start).TotalMilliseconds;
                    ///Rhino.RhinoApp.WriteLine("Info  " + time.ToString());
                }
                foreach (var p in Parameters)
                {
                    //var start = DateTime.Now;
                    p.Render(graphics, canvas);
                    //var time = (DateTime.Now - start).TotalMilliseconds;
                    //Rhino.RhinoApp.WriteLine((p.IsInput ? "Input  " : "Output  " )+ time.ToString());
                }
            }

            public override Region GetClip()
            {
                var clip = new Region(Bounds);
                if (_iconMode)
                    clip.Xor(Info.Bounds);
                foreach (var p in Parameters)
                    clip.Xor(p.Bounds);

                clip.Complement(Bounds);
                return clip;
            }
        }
        internal class RichedCapsuleComponentParameter : RichedCapsule
        {
            public IGH_Component Component { get; }
            public IGH_Param Param { get; }
            public bool IsInput { get; }

            public RichedCapsuleComponentParameter(GH_LinkedParamAttributes att) : base(att)
            {
                Component = Attributes.GetTopLevel.DocObject as IGH_Component;
                Param = Attributes.DocObject as IGH_Param;
                IsInput = Component.Params.IsInputParam(Param);
            }

            protected override RectangleF CreateBounds()
            {
                var att = Attributes as GH_LinkedParamAttributes;
                var box = att.Bounds;
                GH_StateTagList stl = typeof(GH_LinkedParamAttributes)
                    .GetField("m_renderTags", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
                    .GetValue(att) as GH_StateTagList;
                if (stl == null || stl.Count == 0)
                    return box;
                var sBox = stl.BoundingBox;
                if (att.HasInputGrip)
                {
                    return new RectangleF(sBox.Right, box.Y, box.Width - sBox.Width, box.Height);
                }
                else
                {
                    return new RectangleF(box.X, box.Y, box.Width - sBox.Width - 1, box.Height);
                }
            }
            public override void Render(Graphics graphics, GH_Canvas canvas)
            {

                var align = IsInput ? StringAlignment.Far : StringAlignment.Near;
                var nameFormat = new StringFormat() { Alignment = align, LineAlignment = StringAlignment.Center };
                string nameText = Grasshopper.CentralSettings.CanvasFullNames ? Param.Name : Param.NickName;
                var nameFontSize = Remap(ZoomFactor, 0, 1, GH_FontServer.StandardAdjusted.Size, ScaleSize(1.8f));
                var nameFont = GH_FontServer.NewFont(Settings.FontCapsuleParameterName, nameFontSize);
                var nameTextHeight = graphics.MeasureString(nameText, nameFont, (int)Math.Ceiling(Bounds.Width), nameFormat).Height;
                var nameBox = new RectangleF(
                    Bounds.X,
                    Bounds.Y,
                    Bounds.Width,
                    Remap(ZoomFactor, 0, 1, Bounds.Height, nameTextHeight + 1f)
                    );

                var descText = Param.Description;
                switch (Param.Access)
                {
                    case GH_ParamAccess.item:
                        //text = $"(as item) {text}";
                        break;
                    case GH_ParamAccess.list:
                        descText = $"(as list) {descText}";
                        break;
                    case GH_ParamAccess.tree:
                        descText = $"(as tree) {descText}";
                        break;
                }
                var descFormat = new StringFormat() { Alignment = align, LineAlignment = StringAlignment.Near };
                var descFontSize = Settings.FontCapsuleDescription.Size * (1f - 0.4f * ZoomFactor);
                var descFont = GH_FontServer.NewFont(Settings.FontCapsuleDescription, descFontSize, FontStyle.Italic);
                var descBox = new RectangleF(
                    Bounds.X + Padding / 4f,
                    (nameBox.Y + nameBox.Height / 2f) + nameTextHeight / 2f,
                    Bounds.Width,
                    graphics.MeasureString(descText, descFont, (int)Math.Ceiling(Bounds.Width), descFormat).Height
                    );
                var descVisible = ZoomFactor > 0.2f && descBox.Bottom <= Bounds.Bottom;

                var instFont = Settings.FontCapsuleParameterData;
                //var instText = GetDataDescription(Param.VolatileData, graphics, instFont, new SizeF(descBox.Width, Bounds.Height - nameBox.Height - Padding - descBox.Height - Padding), out float intsTextHeight); 
                var instTextHeightLine = graphics.MeasureString("Qq", instFont).Height;
                var instTextLineCount = Param.VolatileDataCount + Param.VolatileData.PathCount + 2;
                var intsTextHeight = instTextLineCount * instTextHeightLine;

                var instBox = new RectangleF(
                    descBox.X,
                    descBox.Bottom,
                    descBox.Width - Padding,
                    intsTextHeight + Padding
                    );
                var instVisible = descVisible && (instBox.Bottom <= Bounds.Bottom || ZoomFactor > 0.5f);

                var iconSize = ZoomFactor >= 1f ? Math.Min(ScaleSize(3f), nameBox.Height) : 0f;
                var iconBox = new RectangleF(
                    IsInput ? descBox.X : descBox.Right - iconSize - Padding / 2f,
                    nameBox.Y + nameBox.Height / 2f - iconSize / 2f,
                    iconSize,
                    iconSize
                    );
                var iconVisible = iconSize >= 1;
                if (iconVisible)
                {
                    if (IsInput)
                    {
                        nameBox.X += iconSize;

                    }
                    nameBox.Width -= iconSize;
                }


                //if (!string.IsNullOrEmpty(Param.NickName) && Param.Name != Param.NickName)
                //{
                //    var nameTextComplete = $"{Param.Name} ({Param.NickName})";
                //    if (graphics.MeasureString(nameTextComplete, nameFont).Width < nameBox.Width)
                //    {
                //        nameText = nameTextComplete;
                //    }
                //    else
                //    {
                //        if (!Grasshopper.CentralSettings.CanvasFullNames && ZoomFactor > 0.5f && graphics.MeasureString(Param.Name, nameFont).Width < nameBox.Width)
                //        {
                //            nameText = Param.Name;
                //        }
                //    }
                //}
                //else
                //{
                //    if (!Grasshopper.CentralSettings.CanvasFullNames && ZoomFactor > 0.5f && graphics.MeasureString(Param.Name, nameFont).Width < nameBox.Width)
                //    {
                //        nameText = Param.Name;
                //    }
                //}

                if (!Grasshopper.CentralSettings.CanvasFullNames && ZoomFactor > 0.5f && graphics.MeasureString(Param.Name, nameFont).Width < nameBox.Width)
                {
                    nameText = Param.Name;
                }

                var totalHeight = nameBox.Height + Padding + descBox.Height + (instVisible ? Padding + Math.Min(instBox.Height, (Bounds.Height - nameBox.Height - Padding - descBox.Height - Padding)) : 0f);

                var dy = ZoomFactor * ((Bounds.Y + Bounds.Height / 2f) - (nameBox.Y + (totalHeight - nameBox.Height / 2f + nameTextHeight / 2f) / 2f));
                iconBox.Offset(0, dy);
                nameBox.Offset(0, dy);
                descBox.Offset(0, dy);
                instBox.Offset(0, dy);
                if (nameBox.Y < Bounds.Y)
                {
                    dy = Bounds.Y - nameBox.Y;
                    iconBox.Offset(0, dy);
                    nameBox.Offset(0, dy);
                    descBox.Offset(0, dy);
                    instBox.Offset(0, dy);
                }

                if (ZoomFactor > 0.5f)
                {
                    descVisible = true;
                    var currentDescBoxHeight = descBox.Height;
                    descBox.Height = Math.Min(descBox.Height, Bounds.Bottom - descBox.Y - Padding / 2f - Math.Min(instBox.Height, ScaleSize(4f)));
                    if (descBox.Height < currentDescBoxHeight)
                    {
                        descFont = AdjustFontHeight(graphics, descFont, descText, descBox);
                    }
                    descVisible = true;

                    instBox.Y = descBox.Bottom + Padding;
                    instBox.Height = Bounds.Bottom - instBox.Y - Padding / 2f;
                    instVisible = instBox.Bottom <= Bounds.Bottom;
                }
                else if (ZoomFactor > 0.2f)
                {
                    var currentDescBoxHeight = descBox.Height;
                    descBox.Height = Bounds.Bottom - descBox.Y - Padding / 2f;
                    if (descBox.Height < currentDescBoxHeight)
                    {
                        descFont = AdjustFontHeight(graphics, descFont, descText, descBox);
                    }
                    descVisible = true;
                    instBox.Y = descBox.Bottom + Padding;
                }

                instVisible &= ZoomFactor >= 0.5;

                if (instVisible)
                {
                    //var lineHeight = graphics.MeasureString("Qy", instFont, (int)Math.Ceiling(descBox.Width)).Height;
                    ///var lineCount = Param.VolatileData.DataDescription(false, true).Split(new[] { "\r\n", "\r", "\n" }, System.StringSplitOptions.None).Length;
                    instBox.Height = Math.Min(instBox.Height, instTextHeightLine * Math.Max(3, instTextLineCount));
                }

                totalHeight = nameBox.Height + (descVisible ? Padding + descBox.Height : 0f) + (instVisible ? Padding + instBox.Height : 0f);

                //if (totalHeight < Bounds.Height)
                //{
                //dy = ZoomFactor * ((Bounds.Y + Bounds.Height / 2f - totalHeight/2f) - (nameBox.Y + nameBox.Height /2f - nameTextHeight/2f));
                //iconBox.Offset(0, dy);
                //nameBox.Offset(0, dy);
                //descBox.Offset(0, dy);
                //instBox.Offset(0, dy);
                //}

                var brushText = new SolidBrush(Style.Text);
                var brushTextFaded = new SolidBrush(FadeColor(Style.Text));

                graphics.DrawString(nameText, nameFont, brushText, nameBox, nameFormat);
                if (iconVisible)
                    graphics.DrawImage(Param.Locked ? Param.Icon_24x24_Locked : Param.Icon_24x24, iconBox);
                if (descVisible)
                    graphics.DrawString(descText, descFont, brushTextFaded, descBox, descFormat);
                if (instVisible)
                    PaintCapsuleParameterData(graphics, instBox, Param, Style);

                /*
                PaintRedBorders(graphics, iconBox);
                PaintRedBorders(graphics, nameBox);
                PaintRedBorders(graphics, descBox);
                PaintRedBorders(graphics, instBox); 
                 */
                nameFont.Dispose();
                descFont.Dispose();
                nameFormat.Dispose();
                brushTextFaded.Dispose();

            }
        }
        internal class RichedCapsuleComponentInfo : RichedCapsule
        {
            public IGH_Component Component { get; }

            public RichedCapsuleComponentInfo(GH_ComponentAttributes att) : base(att)
            {
                Component = Attributes.DocObject as IGH_Component;
            }

            protected override RectangleF CreateBounds()
            {
                var box = ((GH_ComponentAttributes)Attributes).ContentBox;
                box.X = (float)Math.Floor(box.X);
                if (Attributes.DocObject.IconDisplayMode != GH_IconDisplayMode.icon)
                    box.Inflate(1, 1);
                return box;
            }

            public override void Render(Graphics graphics, GH_Canvas canvas)
            {
                var textFormat = new StringFormat() { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Near };

                var iconBox = new RectangleF(
                    Bounds.X + Bounds.Width / 2f - Component.Icon_24x24.Width / 2f,
                    Bounds.Y + Bounds.Height / 2f - Component.Icon_24x24.Height / 2f,
                    Component.Icon_24x24.Width,
                    Component.Icon_24x24.Height
                    );

                var nameText = Component.Name;
                if (!string.IsNullOrEmpty(Component.NickName) && Component.Name != Component.NickName)
                {
                    nameText = $"{nameText} ({Component.NickName})";
                    if (GH_FontServer.MeasureString(nameText, Settings.FontCapsuleInfoName, (int)Math.Ceiling(Bounds.Width)).Height > Bounds.Height)
                    {
                        nameText = Component.Name;
                    }
                }
                var nameBox = new RectangleF(
                    Bounds.X + Padding / 2f,
                    iconBox.Bottom + Padding,
                    Bounds.Width - Padding,
                    graphics.MeasureString(nameText, Settings.FontCapsuleInfoName, (int)Math.Ceiling(Bounds.Width), textFormat).Height
                    );
                var nameVisible = nameBox.Height > 1 && ZoomFactor > 0.1f && nameBox.Bottom <= Bounds.Bottom;

                var descText = Component.Description;
                var descFont = GH_FontServer.NewFont(Settings.FontCapsuleDescription, FontStyle.Italic);
                var descBox = new RectangleF(
                    nameBox.X,
                    nameBox.Bottom + Padding,
                    nameBox.Width,
                    graphics.MeasureString(descText, descFont, (int)Math.Ceiling(Bounds.Width), textFormat).Height
                    );
                var descVisible = nameVisible && ZoomFactor > 0.2f && descBox.Height > 1 && descBox.Bottom <= Bounds.Bottom;

                var instText = Component.InstanceDescription;
                var instFont = Settings.FontCapsuleInstanceDescription;
                var instBox = new RectangleF(
                    descBox.X,
                    descBox.Bottom + Padding,
                    descBox.Width,
                    graphics.MeasureString(instText, instFont, (int)Math.Ceiling(Bounds.Width)).Height + Padding + Padding
                    );
                bool instVisible = descVisible && instBox.Bottom <= Bounds.Bottom;

                var totalHeight = iconBox.Height + Padding + nameBox.Height + Padding + descBox.Height + Padding + instBox.Height;

                var dy = ZoomFactor * ((Bounds.Y + Bounds.Height / 2f) - (iconBox.Y + totalHeight / 2f));
                iconBox.Offset(0, dy);
                nameBox.Offset(0, dy);
                descBox.Offset(0, dy);
                instBox.Offset(0, dy);

                nameVisible = nameBox.Height > 1 && nameBox.Bottom <= Bounds.Bottom;
                descVisible = nameVisible && ZoomFactor > 0.2f && descBox.Height > 1 && descBox.Bottom <= Bounds.Bottom;
                instVisible = descVisible && ZoomFactor >= 0.5f && instBox.Bottom <= Bounds.Bottom;

                if (totalHeight > Bounds.Height)
                {
                    totalHeight -= iconBox.Height;
                    var currentIconBoxBottom = iconBox.Bottom;
                    var idealIconSize = Remap(ZoomFactor, 0, 1, iconBox.Height, Math.Max(ScaleSize(2f), Bounds.Height - totalHeight - Padding));
                    var infl = (iconBox.Height - idealIconSize) / 2f;
                    iconBox.Inflate(-infl, -infl);
                    dy = iconBox.Bottom - currentIconBoxBottom;
                    var dy2 = Bounds.Y + Padding / 2f - iconBox.Y;
                    iconBox.Offset(0, dy2);
                    nameBox.Offset(0, dy + dy2);
                    descBox.Offset(0, dy + dy2);
                    instBox.Offset(0, dy + dy2);
                    nameVisible = nameBox.Height > 1 && nameBox.Bottom <= Bounds.Bottom;
                    descVisible = nameVisible && ZoomFactor > 0.2f && descBox.Height > 1 && descBox.Bottom <= Bounds.Bottom;
                    instVisible = descVisible && ZoomFactor >= 0.5f && instBox.Bottom <= Bounds.Bottom;
                    totalHeight += iconBox.Height;
                    if (nameVisible && descBox.Bottom + Padding > Bounds.Bottom)
                    {
                        instVisible = ZoomFactor >= 1f;
                        var currentDescBoxHeight = descBox.Height;
                        descBox.Height = Bounds.Bottom - descBox.Y - (instVisible ? (instBox.Height + Padding + Padding / 2f) : 0f);
                        if (descBox.Height < currentDescBoxHeight)
                        {
                            descFont = AdjustFontHeight(graphics, descFont, descText, descBox);
                        }
                        descVisible = true;

                        //var fontSizeFactor = Math.Min(1f, (descBox.Height / currentDescBoxHeight));
                        //var fontSize = descFont.Size * fontSizeFactor;
                        //if(fontSize > 0.7f)
                        //{
                        //    if(fontSizeFactor < 1f)
                        //    {
                        //        descFont.Dispose();
                        //        descFont = GH_FontServer.NewFont(Settings.FontCapsuleDescription, fontSize, FontStyle.Italic);
                        //    }
                        //    descVisible = true;

                        //}
                        //else
                        //{
                        //    descVisible = descBox.Height > 3f; 
                        //}

                        if (instVisible)
                        {
                            instBox.Y = descBox.Bottom + Padding;
                        }
                    }

                }

                var textBrush = new SolidBrush(FadeColor(Style.Text));

                graphics.DrawImage(Component.Locked ? Component.Icon_24x24_Locked : Component.Icon_24x24, iconBox);
                if (nameVisible)
                    graphics.DrawString(nameText, Settings.FontCapsuleInfoName, textBrush, nameBox, textFormat);
                if (descVisible)
                    graphics.DrawString(descText, descFont, textBrush, descBox, textFormat);
                if (instVisible)
                {
                    PaintCapsuleBoxBackground(graphics, instBox, Style);
                    PaintCapsuleBox(graphics, instBox, Style);
                    using (var instFormat = new StringFormat() { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center })
                        graphics.DrawString(instText, instFont, textBrush, instBox, instFormat);
                }
                /*
             PaintRedBorders(graphics, iconBox);
             PaintRedBorders(graphics, nameBox);
             PaintRedBorders(graphics, descBox);
             PaintRedBorders(graphics, instBox);
                */
                textFormat.Dispose();
                textBrush.Dispose();
                descFont.Dispose();

            }

        }
        internal class RichedCapsuleParameter : RichedCapsule
        {
            private bool _iconMode;
            public IGH_Param Param { get; }
            public RichedCapsuleParameter(GH_FloatingParamAttributes att) : base(att)
            {
                Param = att.GetTopLevel.DocObject as IGH_Param;
                _iconMode = true;

            }

            public override void Render(Graphics graphics, GH_Canvas canvas)
            {
                if (_iconMode)
                {
                    var half = Param.Icon_24x24.Width / 2f;
                    var nameWidth = Bounds.Width * 0.5f - 1.6f;
                    var nameText = Param.Name;
                    var descText = Param.Description;
                    if (!string.IsNullOrEmpty(Param.NickName) && Param.Name != Param.NickName)
                    {
                        nameText = $"{nameText} ({Param.NickName})";
                        if (GH_FontServer.MeasureString(nameText, Settings.FontCapsuleInfoName, (int)Math.Ceiling(nameWidth)).Height > Bounds.Height)
                        {
                            nameText = Param.Name;
                        }
                    }
                    var textFormat = new StringFormat() { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Near };
                    var nameTextHeight = graphics.MeasureString(nameText, Settings.FontCapsuleInfoName, (int)Math.Ceiling(nameWidth), textFormat).Height; ;
                    var descTextHeight = graphics.MeasureString(descText, Settings.FontCapsuleDescription, (int)Math.Ceiling(nameWidth), textFormat).Height;

                    var iconSize = Remap(ZoomFactor, 0, 1, Param.Icon_24x24.Width, Math.Max(ScaleSize(4f), Bounds.Height - 3.2f - nameTextHeight - Padding - descTextHeight - Padding));
                    var iconBox = new RectangleF(
                        Bounds.X + Remap(ZoomFactor, 0, 1, (Bounds.Width / 2f - half), Bounds.Width / 4f - iconSize / 2f + Padding / 2f),
                        Bounds.Y + 1.6f,
                        iconSize,
                        iconSize
                        );

                    var nameBox = new RectangleF(
                        Bounds.X + 1.6f,
                        iconBox.Bottom + Padding,
                        nameWidth,
                        nameTextHeight
                        );
                    var nameVisible = nameBox.Height > 1 && nameBox.Bottom <= Bounds.Bottom;

                    var descFont = GH_FontServer.NewFont(Settings.FontCapsuleDescription, FontStyle.Italic);
                    var descBox = new RectangleF(
                        nameBox.X,
                        nameBox.Bottom + Padding,
                        nameBox.Width,
                        descTextHeight
                        );
                    var descVisible = nameVisible && descBox.Height > 1 && descBox.Bottom <= Bounds.Bottom;

                    var instBox = new RectangleF(
                        Math.Max(nameBox.Right, iconBox.Right) + Padding,
                        iconBox.Y,
                        Bounds.Right - (nameBox.Right + Padding) - 1.6f,
                        Bounds.Height - 3.2f
                        );
                    var instVisible = instBox.Width > Bounds.Width / 4f;

                    var textBrush = new SolidBrush(FadeColor(Style.Text));

                    graphics.DrawImage(Param.Locked ? Param.Icon_24x24_Locked : Param.Icon_24x24, iconBox);
                    if (nameVisible)
                        graphics.DrawString(nameText, Settings.FontCapsuleInfoName, textBrush, nameBox, textFormat);
                    if (descVisible)
                        graphics.DrawString(descText, descFont, textBrush, descBox, textFormat);
                    if (instVisible)
                        PaintCapsuleParameterData(graphics, instBox, Param, Style);

                    /*
                    PaintRedBorders(graphics, iconBox);
                    PaintRedBorders(graphics, nameBox);
                    PaintRedBorders(graphics, descBox);
                    PaintRedBorders(graphics, instBox);
                    */

                    textFormat.Dispose();
                    textBrush.Dispose();
                    descFont.Dispose();
                }
                else
                {

                }
            }
        }
    }


}
