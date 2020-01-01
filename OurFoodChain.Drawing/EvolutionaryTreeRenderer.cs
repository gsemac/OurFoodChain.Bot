﻿using OurFoodChain.Common;
using OurFoodChain.Common.Collections;
using OurFoodChain.Common.Extensions;
using OurFoodChain.Taxa;
using SkiaSharp;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;

namespace OurFoodChain.Drawing {

    public class EvolutionaryTreeRenderer :
        IEvolutionaryTreeRenderer {

        // Public members

        public ISpecies HighlightedSpecies { get; set; }

        public void SaveTo(TreeNode<ISpecies> inputTree, Stream stream) {

            // Copy the evolutionary tree.

            TreeNode<NodeData> root = inputTree.Copy(n => new NodeData {
                Species = n
            });

            using (SKPaint paint = new SKPaint {
                Typeface = SKTypeface.FromFamilyName("Calibri"),
                TextSize = 12.0f
            }) {

                // Measure the size of each node.

                float nodePaddingX = 5.0f;

                root.PostOrderTraverse(node => {

                    float textHeight = paint.FontMetrics.XHeight;
                    float textWidth = paint.MeasureText(node.Value.Species.BinomialName.ToString(BinomialNameFormat.Abbreviated));

                    node.Value.Bounds = new RectangleF(node.Value.Bounds.X, node.Value.Bounds.Y, textWidth + nodePaddingX, textHeight);

                });

                // Calculate positions for each node.

                CalculateNodePositions(root);

                // Calculate the size of the tree.

                RectangleF bounds = CalculateTreeBounds(root);

                // Shift the tree so that it is within the bounds of the image.

                float minX = 0.0f;

                root.PostOrderTraverse(node => {

                    if (node.Value.Bounds.X < minX)
                        minX = bounds.X;

                });

                ShiftTree(root, -minX, 0.0f);

                // Create the image.

                SKImageInfo imageInfo = new SKImageInfo((int)bounds.Width, (int)bounds.Height);

                using (SKSurface surface = SKSurface.Create(imageInfo)) {

                    SKCanvas canvas = surface.Canvas;

                    canvas.Clear(new SKColor(54, 57, 63));

                    paint.IsAntialias = true;

                    DrawNode(canvas, root, paint, HighlightedSpecies);

                    // Write the result to the stream.

                    using (SKImage image = surface.Snapshot())
                    using (SKData data = image.Encode(SKEncodedImageFormat.Png, 100))
                        data.SaveTo(stream);

                }

            }

        }

        // Private members

        private class NodeData {

            public ISpecies Species { get; set; }
            public RectangleF Bounds { get; set; } = new RectangleF();

        }

        private class NodeBoundsPair {

            public TreeNode<NodeData> Node { get; set; }
            public RectangleF Bounds { get; set; } = new RectangleF();

        }

        private static void CalculateNodePositions(TreeNode<NodeData> node) {

            InitializeNodePositions(node);

            CorrectSubtreeOverlap(node);

        }
        private static void InitializeNodePositions(TreeNode<NodeData> node) {

            // Get the widths of this node's children, and space them out evenly.

            float totalWidth = 0.0f;

            foreach (TreeNode<NodeData> child in node.Children)
                totalWidth += child.Value.Bounds.Width;

            float childX = node.Value.Bounds.X + (node.Value.Bounds.Width / 2.0f) - (totalWidth / 2.0f);
            float childY = node.Value.Bounds.Y + node.Value.Bounds.Height;

            float vertical_spacing = node.Value.Bounds.Height * 1.5f;

            foreach (TreeNode<NodeData> child in node.Children) {

                child.Value.Bounds = new RectangleF(childX, childY + vertical_spacing, child.Value.Bounds.Width, child.Value.Bounds.Height);

                childX += child.Value.Bounds.Width;

            }

            // Do this for each child node as well.

            foreach (TreeNode<NodeData> child in node.Children)
                CalculateNodePositions(child);

        }
        private static void CorrectSubtreeOverlap(TreeNode<NodeData> node) {

            node.PostOrderTraverse(n => {

                // Check if this node has overlapping subtrees.

                if (n.Children.Count() <= 1)
                    return;

                bool has_overlapping = false;
                List<NodeBoundsPair> bounds = new List<NodeBoundsPair>();

                foreach (var child in node.Children)
                    bounds.Add(new NodeBoundsPair {
                        Node = child,
                        Bounds = CalculateTreeBounds(child)
                    });

                for (int i = 1; i < bounds.Count(); ++i) {

                    if (bounds[i].Bounds.Left < bounds[i - 1].Bounds.Right) {
                        has_overlapping = true;

                        break;

                    }

                }

                // If there are overlapping subtrees, move them so that they are no longer overlapping.

                if (has_overlapping) {

                    int left = 0;
                    int right = 0;

                    if (bounds.Count() % 2 == 1) {

                        int middle = (int)Math.Ceiling(bounds.Count() / 2.0f);

                        left = middle - 1;
                        right = middle + 1;

                    }
                    else {

                        left = (bounds.Count() / 2) - 1;
                        right = bounds.Count() / 2;

                    }

                    for (int i = left; i >= 0; --i) {

                        float overlap = bounds[i].Bounds.Right - bounds[i + 1].Bounds.Left;

                        if (overlap <= 0.0f)
                            continue;

                        if (i == left)
                            overlap /= 2.0f;

                        bounds[i].Bounds = new RectangleF(
                            bounds[i].Bounds.X - overlap,
                            bounds[i].Bounds.Y,
                            bounds[i].Bounds.Width,
                            bounds[i].Bounds.Height
                            );

                        ShiftTree(bounds[i].Node, -overlap, 0.0f);

                    }

                    for (int i = right; i < bounds.Count(); ++i) {

                        float overlap = bounds[i - 1].Bounds.Right - bounds[i].Bounds.Left;

                        if (overlap <= 0.0f)
                            continue;

                        if (i == right)
                            overlap /= 2.0f;

                        bounds[i].Bounds = new RectangleF(
                            bounds[i].Bounds.X + overlap,
                            bounds[i].Bounds.Y,
                            bounds[i].Bounds.Width,
                            bounds[i].Bounds.Height
                            );

                        ShiftTree(bounds[i].Node, overlap, 0.0f);

                    }

                }

                // Finally, center the parent node over its children.

                float child_min_x = node.Children.First().Value.Bounds.X;
                float child_max_x = node.Children.Last().Value.Bounds.X + node.Children.Last().Value.Bounds.Width;
                float child_width = child_max_x - child_min_x;

                node.Value.Bounds = new RectangleF(
                    child_min_x + (child_width / 2.0f) - (node.Value.Bounds.Width / 2.0f),
                    node.Value.Bounds.Y,
                    node.Value.Bounds.Width,
                    node.Value.Bounds.Height
                    );

            });

        }
        private static RectangleF CalculateTreeBounds(TreeNode<NodeData> node) {

            float min_x = node.Value.Bounds.X;
            float max_x = node.Value.Bounds.X + node.Value.Bounds.Width;
            float min_y = node.Value.Bounds.Y;
            float max_y = node.Value.Bounds.Y + node.Value.Bounds.Height;

            foreach (TreeNode<NodeData> child in node.Children) {

                RectangleF bounds = CalculateTreeBounds(child);

                min_x = Math.Min(bounds.X, min_x);
                max_x = Math.Max(bounds.X + bounds.Width, max_x);
                min_y = Math.Min(bounds.Y, min_y);
                max_y = Math.Max(bounds.Y + bounds.Height, max_y);

            }

            return new RectangleF(min_x, min_y, max_x - min_x, max_y - min_y);

        }
        private static void ShiftTree(TreeNode<NodeData> root, float deltaX, float deltaY) {

            root.PostOrderTraverse(node => {

                node.Value.Bounds = new RectangleF(
                    node.Value.Bounds.X + deltaX,
                    node.Value.Bounds.Y + deltaY,
                    node.Value.Bounds.Width,
                    node.Value.Bounds.Height
                    );

            });

        }

        private static void DrawNode(SKCanvas canvas, TreeNode<NodeData> node, SKPaint paint, ISpecies highlightedSpecies) {

            // Cross-out the species if it's extinct.

            if (node.Value.Species.Status.IsExinct) {

                paint.Color = SKColors.White;
                paint.StrokeWidth = 1.0f;

                canvas.DrawLine(
                    new SKPoint(node.Value.Bounds.X, node.Value.Bounds.Y + node.Value.Bounds.Height / 2.0f),
                    new SKPoint(node.Value.Bounds.X + node.Value.Bounds.Width - 5.0f, node.Value.Bounds.Y + node.Value.Bounds.Height / 2.0f),
                    paint
                    );

            }

            // Draw the name of the species.

            paint.Color = (highlightedSpecies != null && node.Value.Species.Id == highlightedSpecies.Id) ? SKColors.Yellow : SKColors.White;

            canvas.DrawText(node.Value.Species.ShortName, node.Value.Bounds.X, node.Value.Bounds.Y, paint);

            // Draw child nodes.

            foreach (TreeNode<NodeData> child in node.Children) {

                paint.StrokeCap = SKStrokeCap.Round;
                paint.StrokeWidth = 2.0f;

                canvas.DrawLine(
                        new SKPoint(node.Value.Bounds.X + (node.Value.Bounds.Width / 2.0f), node.Value.Bounds.Y + node.Value.Bounds.Height),
                        new SKPoint(child.Value.Bounds.X + (child.Value.Bounds.Width / 2.0f), child.Value.Bounds.Y),
                        paint
                        );

                DrawNode(canvas, child, paint, highlightedSpecies);

            }

        }

    }

}