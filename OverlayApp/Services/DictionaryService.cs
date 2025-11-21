using Microsoft.Data.Sqlite;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using OverlayApp.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OverlayApp.Services
{
    public class DictionaryService
    {
        private const string DB_NAME = "dict.db";
        public bool IsLoaded { get; private set; } = false;
        public event Action<string>? StatusUpdate;

        public async Task InitializeAsync(string zipPath)
        {
            // 1. CHECK FOR OLD DATABASE FORMAT & NUKE IT IF NECESSARY
            if (File.Exists(DB_NAME))
            {
                if (await IsDatabaseOutdated())
                {
                    StatusUpdate?.Invoke("Updating database structure...");
                    SqliteConnection.ClearAllPools(); // Release file lock
                    File.Delete(DB_NAME);
                }
                else
                {
                    StatusUpdate?.Invoke("Database found. Loading...");
                    IsLoaded = true;
                    return;
                }
            }

            // 2. CREATE NEW DATABASE
            StatusUpdate?.Invoke("Importing Dictionary (This will take ~1 min)...");
            if (!File.Exists(zipPath)) throw new FileNotFoundException("jitendex.zip missing");

            await Task.Run(() =>
            {
                using (var connection = new SqliteConnection($"Data Source={DB_NAME}"))
                {
                    connection.Open();
                    using (var cmd = connection.CreateCommand())
                    {
                        cmd.CommandText = @"
                            CREATE TABLE IF NOT EXISTS entries (
                                term TEXT,
                                reading TEXT,
                                score INTEGER,
                                json_data TEXT
                            );
                            CREATE INDEX IF NOT EXISTS idx_term ON entries(term);
                            CREATE INDEX IF NOT EXISTS idx_reading ON entries(reading);
                            CREATE INDEX IF NOT EXISTS idx_score ON entries(score);
                        ";
                        cmd.ExecuteNonQuery();
                    }

                    using (var transaction = connection.BeginTransaction())
                    {
                        using (FileStream fs = new FileStream(zipPath, FileMode.Open, FileAccess.Read))
                        using (ZipArchive archive = new ZipArchive(fs, ZipArchiveMode.Read))
                        {
                            int count = 0;
                            foreach (var entry in archive.Entries)
                            {
                                if (entry.Name.StartsWith("term_bank") && entry.Name.EndsWith(".json"))
                                {
                                    ProcessJsonFile(entry, connection, transaction);
                                    count++;
                                    StatusUpdate?.Invoke($"Processing bank {count}...");
                                }
                            }
                        }
                        transaction.Commit();
                    }
                }
                IsLoaded = true;
            });
        }

        private async Task<bool> IsDatabaseOutdated()
        {
            try
            {
                using (var connection = new SqliteConnection($"Data Source={DB_NAME}"))
                {
                    connection.Open();
                    var cmd = connection.CreateCommand();
                    cmd.CommandText = "SELECT json_data FROM entries LIMIT 1";
                    var result = await cmd.ExecuteScalarAsync();
                    if (result is string s)
                    {
                        // If it starts with '[', it's the OLD array format. We want '{' (Object).
                        if (s.TrimStart().StartsWith("[")) return true;
                    }
                }
                return false;
            }
            catch
            {
                return true; // If error reading, treat as outdated
            }
        }

        private void ProcessJsonFile(ZipArchiveEntry entry, SqliteConnection conn, SqliteTransaction trans)
        {
            using (var stream = entry.Open())
            using (var reader = new StreamReader(stream))
            using (var jsonReader = new JsonTextReader(reader))
            {
                var termBank = JArray.Load(jsonReader);
                var command = conn.CreateCommand();
                command.Transaction = trans;
                command.CommandText = "INSERT INTO entries (term, reading, score, json_data) VALUES ($term, $reading, $score, $json)";

                var pTerm = command.CreateParameter(); pTerm.ParameterName = "$term"; command.Parameters.Add(pTerm);
                var pRead = command.CreateParameter(); pRead.ParameterName = "$reading"; command.Parameters.Add(pRead);
                var pScore = command.CreateParameter(); pScore.ParameterName = "$score"; command.Parameters.Add(pScore);
                var pJson = command.CreateParameter(); pJson.ParameterName = "$json"; command.Parameters.Add(pJson);

                foreach (var item in termBank)
                {
                    if (item is not JArray arr || arr.Count < 6) continue;

                    pTerm.Value = arr[0].ToString();
                    pRead.Value = arr[1].ToString();

                    int score = 0;
                    int.TryParse(arr[4].ToString(), out score);
                    pScore.Value = score;

                    var senses = new List<Sense>();
                    var mainTags = arr[2].ToString().Split(' ', StringSplitOptions.RemoveEmptyEntries).ToList();

                    if (arr[5] is JArray defArray)
                    {
                        foreach (var d in defArray)
                        {
                            var sense = ParseSense(d, mainTags);
                            senses.Add(sense);
                        }
                    }

                    // Store wrapper object
                    var storageObj = new
                    {
                        tags = mainTags,
                        defs = arr[5],
                        senses = senses
                    };

                    pJson.Value = JsonConvert.SerializeObject(storageObj);
                    command.ExecuteNonQuery();
                }
            }
        }

        private Sense ParseSense(JToken token, List<string> defaultTags)
        {
            var sense = new Sense();
            sense.PoSTags.AddRange(defaultTags);

            if (token.Type == JTokenType.String) sense.Glossaries.Add(token.ToString());
            else if (token is JObject obj) ParseComplexNode(obj, sense);
            return sense;
        }

        private void ParseComplexNode(JToken token, Sense sense)
        {
            if (token is JArray arr)
            {
                foreach (var child in arr) ParseComplexNode(child, sense);
                return;
            }
            if (token is not JObject obj) return;

            string tag = obj["tag"]?.ToString() ?? "";
            var data = obj["data"];
            var content = obj["content"];

            if (tag == "span" && (data?["tag"] != null || data?["type"]?.ToString() == "word-class"))
            {
                string t = GetPlainString(content);
                if (!string.IsNullOrWhiteSpace(t) && !sense.PoSTags.Contains(t)) sense.PoSTags.Add(t);
            }
            else if (tag == "div" && data?["type"]?.ToString() == "example")
            {
                string rawEx = GetPlainString(content);
                var ex = new ExampleSentence { Japanese = rawEx };
                int slashIdx = rawEx.LastIndexOf(" / ");
                if (slashIdx > 0)
                {
                    ex.Japanese = rawEx.Substring(0, slashIdx).Trim();
                    ex.English = rawEx.Substring(slashIdx + 3).Trim();
                }
                sense.Examples.Add(ex);
            }
            else if (tag == "div" && data?["content"]?.ToString() == "notes")
            {
                string info = GetPlainString(content);
                if (!string.IsNullOrWhiteSpace(info)) sense.Info.Add(info);
            }
            else if (content != null)
            {
                if (content.Type == JTokenType.String) sense.Glossaries.Add(content.ToString());
                else if (content is JArray)
                {
                    string s = GetPlainString(content);
                    if (!string.IsNullOrWhiteSpace(s)) sense.Glossaries.Add(s);
                }
            }
        }

        private string GetPlainString(JToken? token)
        {
            if (token == null) return "";
            if (token.Type == JTokenType.String) return token.ToString();
            if (token is JArray arr)
            {
                StringBuilder sb = new StringBuilder();
                foreach (var c in arr) sb.Append(GetPlainString(c));
                return sb.ToString();
            }
            if (token is JObject obj && obj["content"] != null) return GetPlainString(obj["content"]);
            return "";
        }

        public List<DictionaryEntry> Lookup(string word)
        {
            var results = new List<DictionaryEntry>();
            if (!IsLoaded || string.IsNullOrEmpty(word)) return results;

            using (var connection = new SqliteConnection($"Data Source={DB_NAME}"))
            {
                connection.Open();
                var command = connection.CreateCommand();
                command.CommandText = "SELECT term, reading, score, json_data FROM entries WHERE term = $w OR reading = $w ORDER BY score DESC LIMIT 10";
                command.Parameters.AddWithValue("$w", word);

                using (var reader = command.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        var entry = new DictionaryEntry
                        {
                            Headword = reader.GetString(0),
                            Reading = reader.GetString(1),
                            Score = reader.GetInt32(2)
                        };

                        try
                        {
                            string jsonStr = reader.GetString(3);
                            var jsonObj = JObject.Parse(jsonStr);

                            if (jsonObj["senses"] is JToken sensesToken)
                            {
                                entry.Senses = sensesToken.ToObject<List<Sense>>() ?? new List<Sense>();
                            }

                            var tags = jsonObj["tags"]?.ToObject<List<string>>();
                            var defs = jsonObj["defs"];
                            entry.DefinitionDocument = JitendexParser.ParseToFlowDocument(defs, tags);
                        }
                        catch { }

                        results.Add(entry);
                    }
                }
            }
            return results;
        }
    }
}