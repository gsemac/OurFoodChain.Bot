using OurFoodChain.Common.Collections;
using OurFoodChain.Common.Taxa;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;

namespace OurFoodChain.Drawing {

    public class StraightLineCladogramRenderer :
        DefaultCladogramRenderer {

        // Public members

        public StraightLineCladogramRenderer(ICladogram cladogram) :
            base(cladogram) {
        }

        // Protected members

        protected override void RenderChildNodes(ICanvas canvas, TreeNode<NodeData> node, Font font) {

            if (node.Children.Any()) {

                var ancestorChildNode = node.Children.Where(child => child.Value.Data.IsAncestor).FirstOrDefault();

                float midY = node.Value.Bounds.Y + node.Value.Bounds.Height + ((node.Children.First().Value.Bounds.Y - (node.Value.Bounds.Y + node.Value.Bounds.Height)) / 2.0f);

                using (Brush highlightBrush = new SolidBrush(HighlightColor))
                using (Brush brush = new SolidBrush(TextColor))
                using (Pen pen = new Pen(brush, 2.0f))
                using (Pen highlightPen = new Pen(highlightBrush, 3.0f)) {

                    pen.StartCap = System.Drawing.Drawing2D.LineCap.Round;
                    highlightPen.StartCap = System.Drawing.Drawing2D.LineCap.Round;
                    pen.EndCap = System.Drawing.Drawing2D.LineCap.Round;
                    highlightPen.EndCap = System.Drawing.Drawing2D.LineCap.Round;

                    pen.Alignment = System.Drawing.Drawing2D.PenAlignment.Center;
                    highlightPen.Alignment = System.Drawing.Drawing2D.PenAlignment.Center;

                    // Draw the vertical line going down from the parent node.

                    float startX = node.Value.Bounds.X + (node.Value.Bounds.Width / 2.0f);
                    float startY = node.Value.Bounds.Y + node.Value.Bounds.Height;

                    float endX = startX;
                    float endY = midY;

                    canvas.DrawLine(pen, new PointF(startX, startY), new PointF(endX, endY));

                    if (ancestorChildNode != null)
                        canvas.DrawLine(highlightPen, new PointF(startX, startY), new PointF(endX, endY));

                    // Draw the horizontal line going across all child nodes.

                    TreeNode<NodeData> leftmostChildNode = node.Children.First();
                    TreeNode<NodeData> rightmostChildNode = node.Children.Last();

                    startX = leftmostChildNode.Value.Bounds.X + (leftmostChildNode.Value.Bounds.Width / 2.0f);
                    startY = endY;

                    endX = rightmostChildNode.Value.Bounds.X + (rightmostChildNode.Value.Bounds.Width / 2.0f);

                    canvas.DrawLine(pen, new PointF(startX, startY), new PointF(endX, endY));

                    if(ancestorChildNode != null) {

                        startX = node.Value.Bounds.X + (node.Value.Bounds.Width / 2.0f);
                        endX = ancestorChildNode.Value.Bounds.X + (ancestorChildNode.Value.Bounds.Width / 2.0f);

                        canvas.DrawLine(highlightPen, new PointF(startX, startY), new PointF(endX, endY));

                    }

                    pen.EndCap = System.Drawing.Drawing2D.LineCap.ArrowAnchor;
                    highlightPen.EndCap = System.Drawing.Drawing2D.LineCap.ArrowAnchor;

                    foreach (TreeNode<NodeData> child in node.Children) {

                        // Draw the vertical line going down to the child node.

                        startX = child.Value.Bounds.X + (child.Value.Bounds.Width / 2.0f);
                        startY = midY;

                        endX = startX;
                        endY = node.Children.First().Value.Bounds.Y;

                        canvas.DrawLine(pen, new PointF(startX, startY), new PointF(endX, endY));

                        if(child == ancestorChildNode)
                            canvas.DrawLine(highlightPen, new PointF(startX, startY), new PointF(endX, endY));

                        //float startX = node.Value.Bounds.X + (node.Value.Bounds.Width / 2.0f);

                        //float startY = node.Value.Bounds.Y + node.Value.Bounds.Height;
                        //float endY = (node.Value.Bounds.Y - child.Value.Bounds.Y) / 2.0f;


                        //pen.EndCap = System.Drawing.Drawing2D.LineCap.ArrowAnchor;

                        ////canvas.DrawLine(pen,
                        ////    new PointF(node.Value.Bounds.X + (node.Value.Bounds.Width / 2.0f), node.Value.Bounds.Y + node.Value.Bounds.Height),
                        ////    new PointF(child.Value.Bounds.X + (child.Value.Bounds.Width / 2.0f), child.Value.Bounds.Y));

                        RenderNode(canvas, child, font);

                    }

                }

            }

        }

    }

}