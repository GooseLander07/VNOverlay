using System;
using System.Collections.Generic;
using System.IO;
using NMeCab;
using OverlayApp.Models;

namespace OverlayApp.Services
{
    public class TextAnalyzer
    {
        private MeCabTagger _tagger;

        public TextAnalyzer()
        {
            // 1. Get the Absolute Path to 'dic/ipadic'
            string baseDir = AppDomain.CurrentDomain.BaseDirectory;
            string dicPath = Path.Combine(baseDir, "dic", "ipadic");

            // 2. Sanity Check
            if (!Directory.Exists(dicPath))
            {
                throw new DirectoryNotFoundException($"Dictionary folder missing at:\n{dicPath}");
            }

            // 3. THE FIX: Pass ONLY the Path
            // The error showed that MeCab treats the input string as the directory.
            // So we give it exactly that. No "-d", no "-r", no quotes.
            // We just ensure backslashes are valid.

            // Using Forward Slashes is safest for cross-compatibility in C++ libs
            string mecabArg = dicPath.Replace("\\", "/");

            try
            {
                _tagger = MeCabTagger.Create(mecabArg);
            }
            catch (Exception ex)
            {
                // If that fails, we try one desperate fallback: empty string
                // This forces MeCab to look in default locations (registry/current dir)
                try
                {
                    _tagger = MeCabTagger.Create("");
                }
                catch
                {
                    throw new Exception($"MeCab initialization failed. It rejected path: {mecabArg}\nError: {ex.Message}");
                }
            }
        }

        public List<Token> Analyze(string text)
        {
            var tokens = new List<Token>();
            if (string.IsNullOrWhiteSpace(text)) return tokens;

            var nodes = _tagger.Parse(text);

            foreach (var node in nodes)
            {
                bool isBos = node.Stat == MeCabNodeStat.Bos;
                bool isEos = node.Stat == MeCabNodeStat.Eos;

                if (!isBos && !isEos)
                {
                    string featureStr = node.Feature?.ToString() ?? "";
                    string[] features = featureStr.Split(',');

                    var token = new Token
                    {
                        Surface = node.Surface,
                        PartOfSpeech = features.Length > 0 ? features[0] : "Unknown",
                        IsWord = features.Length > 0 && features[0] != "‹L†"
                    };

                    if (features.Length > 6)
                    {
                        string root = features[6];
                        token.OriginalForm = (root == "*") ? node.Surface : root;
                    }
                    else
                    {
                        token.OriginalForm = node.Surface;
                    }

                    if (features.Length > 7)
                    {
                        token.Reading = features[7];
                    }

                    tokens.Add(token);
                }
            }

            return tokens;
        }
    }
}