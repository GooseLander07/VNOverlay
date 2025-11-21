using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Media;
using Newtonsoft.Json.Linq;

namespace OverlayApp.Services
{
    public static class JitendexParser
    {
        // CRITICAL FIX: Use direct Colors instead of Converter to prevent TypeInitializationException
        private static readonly Brush ColorPos = new SolidColorBrush(Color.FromRgb(86, 86, 86));   // #565656
        private static readonly Brush ColorMisc = Brushes.Brown;
        private static readonly Brush ColorExampleKey = Brushes.LimeGreen;
        private static readonly Brush ColorText = Brushes.WhiteSmoke;
        private static readonly Brush ColorSubText = Brushes.Gray;

        public static FlowDocument ParseToFlowDocument(JToken? definitionsArray, List<string>? topTags)
        {
            var doc = new FlowDocument
            {
                PagePadding = new Thickness(0),
                Background = Brushes.Transparent,
                Foreground = ColorText,
                FontFamily = new FontFamily("Segoe UI, Yu Gothic UI"),
                FontSize = 14
            };

            // 1. Add Top Tags
            if (topTags != null && topTags.Count > 0)
            {
                var tagPara = new Paragraph();
                foreach (var tag in topTags)
                {
                    if (!string.IsNullOrEmpty(tag))
                    {
                        tagPara.Inlines.Add(CreateBadge(tag, ColorPos));
                        tagPara.Inlines.Add(new Run(" "));
                    }
                }
                doc.Blocks.Add(tagPara);
            }

            // 2. Process Definitions
            if (definitionsArray is JArray arr)
            {
                foreach (var item in arr)
                {
                    if (item is JObject obj && obj["type"]?.ToString() == "structured-content")
                    {
                        var blocks = ParseBlock(obj["content"]);
                        foreach (var b in blocks) doc.Blocks.Add(b);
                    }
                    else if (item.Type == JTokenType.String)
                    {
                        doc.Blocks.Add(new Paragraph(new Run("• " + item.ToString())));
                    }
                }
            }

            return doc;
        }

        private static List<Block> ParseBlock(JToken? token)
        {
            var blocks = new List<Block>();
            if (token == null) return blocks;

            if (token is JArray arr)
            {
                foreach (var child in arr) blocks.AddRange(ParseBlock(child));
            }
            else if (token is JObject obj)
            {
                string? tag = obj["tag"]?.ToString();
                var content = obj["content"];
                var data = obj["data"];

                if (tag == "ul" || tag == "ol")
                {
                    var list = new List();
                    list.MarkerStyle = tag == "ul" ? TextMarkerStyle.Disc : TextMarkerStyle.Decimal;
                    list.Padding = new Thickness(20, 0, 0, 0);

                    var items = new List<JToken>();
                    if (content is JArray a) items.AddRange(a);
                    else if (content is JObject o) items.Add(o);

                    foreach (var li in items)
                    {
                        var listItem = new ListItem();
                        var childBlocks = ParseBlock(li);
                        foreach (var b in childBlocks) listItem.Blocks.Add(b);
                        list.ListItems.Add(listItem);
                    }
                    blocks.Add(list);
                }
                else if (tag == "li")
                {
                    blocks.AddRange(ParseBlock(content));
                }
                else if (tag == "div")
                {
                    if (data?["content"]?.ToString() == "example-sentence")
                    {
                        var p = new Paragraph { Margin = new Thickness(10, 5, 0, 5) };
                        ParseInline(content, p.Inlines);
                        blocks.Add(p);
                    }
                    else
                    {
                        if (HasBlockChildren(content))
                            blocks.AddRange(ParseBlock(content));
                        else
                        {
                            var p = new Paragraph();
                            ParseInline(content, p.Inlines);
                            if (p.Inlines.Count > 0) blocks.Add(p);
                        }
                    }
                }
                else
                {
                    var p = new Paragraph();
                    ParseInline(token, p.Inlines);
                    if (p.Inlines.Count > 0) blocks.Add(p);
                }
            }
            else if (token.Type == JTokenType.String)
            {
                blocks.Add(new Paragraph(new Run(token.ToString())));
            }

            return blocks;
        }

        private static void ParseInline(JToken? token, InlineCollection inlines, Brush? inheritedForeground = null)
        {
            if (token == null) return;

            // 1. Handle Arrays (Recursion)
            if (token is JArray arr)
            {
                foreach (var child in arr) ParseInline(child, inlines, inheritedForeground);
                return;
            }

            // 2. Handle Strings (Simple Text)
            if (token.Type == JTokenType.String)
            {
                var run = new Run(token.ToString());
                if (inheritedForeground != null) run.Foreground = inheritedForeground;
                inlines.Add(run);
                return;
            }

            // 3. Handle Objects (Spans, Ruby, Links)
            if (token is JObject obj)
            {
                string? tag = obj["tag"]?.ToString();
                var content = obj["content"];
                var data = obj["data"];

                if (tag == "span")
                {
                    string? type = data?["content"]?.ToString();

                    if (type == "part-of-speech-info")
                        inlines.Add(CreateBadge(GetContentString(content), ColorPos));
                    else if (type == "misc-info")
                        inlines.Add(CreateBadge(GetContentString(content), ColorMisc));
                    else if (type == "example-keyword")
                    {
                        // Keyword detected: Pass the GREEN color down to children
                        var span = new Span();
                        ParseInline(content, span.Inlines, ColorExampleKey);
                        inlines.Add(span);
                    }
                    else
                    {
                        var span = new Span();
                        ParseInline(content, span.Inlines, inheritedForeground);
                        inlines.Add(span);
                    }
                }
                else if (tag == "ruby")
                {
                    // --- FIXED RUBY LOGIC (Pairwise Parsing) ---
                    if (content is JArray parts)
                    {
                        // We use a Span to hold multiple InlineUIContainers side-by-side
                        var containerSpan = new Span();

                        for (int i = 0; i < parts.Count; i++)
                        {
                            var currentItem = parts[i];

                            // Check if this item is a Reading Tag (RT)
                            // If it is, we skip it because it should have been handled by the previous Base item.
                            // However, if we find a stray RT, we ignore it to prevent crashes.
                            if (IsRubyTag(currentItem)) continue;

                            // Get the Base Text (Kanji)
                            // It might be a simple string or a nested object (like a span)
                            // We need to extract the raw text for the display
                            string baseText = GetContentString(currentItem);

                            // LOOK AHEAD: Is the NEXT item a Reading (RT)?
                            string rtText = "";
                            if (i + 1 < parts.Count && IsRubyTag(parts[i + 1]))
                            {
                                rtText = GetContentString(parts[i + 1]["content"]);
                            }

                            // Create the Vertical Stack
                            var stack = new StackPanel
                            {
                                Orientation = Orientation.Vertical,
                                VerticalAlignment = VerticalAlignment.Bottom,
                                Margin = new Thickness(0, 0, 0, 3)
                            };

                            // Top: Furigana (Reading)
                            if (!string.IsNullOrEmpty(rtText))
                            {
                                stack.Children.Add(new TextBlock
                                {
                                    Text = rtText,
                                    FontSize = 9,
                                    Foreground = inheritedForeground ?? Brushes.Gray, // Inherit Green if keyword
                                    HorizontalAlignment = HorizontalAlignment.Center,
                                    TextAlignment = TextAlignment.Center,
                                    Margin = new Thickness(0, 0, 0, -2) // Pull closer to Kanji
                                });
                            }

                            // Bottom: Kanji (Base)
                            stack.Children.Add(new TextBlock
                            {
                                Text = baseText,
                                FontSize = 14,
                                Foreground = inheritedForeground ?? ColorText,
                                HorizontalAlignment = HorizontalAlignment.Center,
                                TextAlignment = TextAlignment.Center
                            });

                            containerSpan.Inlines.Add(new InlineUIContainer(stack) { BaselineAlignment = BaselineAlignment.Bottom });
                        }

                        inlines.Add(containerSpan);
                    }
                }
                else if (tag == "a")
                {
                    var link = new Span();
                    link.Foreground = new SolidColorBrush(Color.FromRgb(100, 181, 246));
                    ParseInline(content, link.Inlines, link.Foreground);
                    inlines.Add(link);
                }
                else
                {
                    ParseInline(content, inlines, inheritedForeground);
                }
            }
        }

        // Helper to check if a JSON token is an RT tag
        private static bool IsRubyTag(JToken? token)
        {
            if (token is JObject o && o["tag"]?.ToString() == "rt") return true;
            return false;
        }

        private static InlineUIContainer CreateBadge(string text, Brush bg)
        {
            if (string.IsNullOrEmpty(text)) text = "?";
            var border = new Border
            {
                Background = bg,
                CornerRadius = new CornerRadius(3),
                Padding = new Thickness(4, 1, 4, 1),
                Margin = new Thickness(0, 0, 4, 0),
                VerticalAlignment = VerticalAlignment.Center
            };
            border.Child = new TextBlock { Text = text, Foreground = Brushes.White, FontSize = 11, FontWeight = FontWeights.Bold };
            return new InlineUIContainer(border) { BaselineAlignment = BaselineAlignment.Center };
        }

        private static string GetContentString(JToken? token)
        {
            if (token == null) return "";
            if (token.Type == JTokenType.String) return token.ToString();
            if (token is JArray arr)
            {
                string s = "";
                foreach (var c in arr) s += GetContentString(c);
                return s;
            }
            if (token is JObject obj) return GetContentString(obj["content"]);
            return "";
        }

        private static bool HasBlockChildren(JToken? token)
        {
            if (token is JArray arr)
            {
                foreach (var t in arr) if (HasBlockChildren(t)) return true;
            }
            if (token is JObject obj)
            {
                string? tag = obj["tag"]?.ToString();
                return tag == "ul" || tag == "ol" || tag == "div" || tag == "li";
            }
            return false;
        }
    }
}