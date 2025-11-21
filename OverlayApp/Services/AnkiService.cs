using System;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace OverlayApp.Services
{
    public class AnkiService
    {
        private const string ANKI_URL = "http://127.0.0.1:8765";
        private readonly HttpClient _client = new HttpClient();

        public async Task<bool> CheckConnection()
        {
            try { return await Post("version") != null; } catch { return false; }
        }

        public async Task<string> AddNote(string deck, string expression, string reading, string meaning, string sentence)
        {
            // 1. Beautify Sentence: Bold the word in the sentence
            string highlightSentence = sentence;
            if (!string.IsNullOrEmpty(expression) && sentence.Contains(expression))
            {
                highlightSentence = sentence.Replace(expression, $"<b style='color: #ff8c00;'>{expression}</b>");
            }

            // 2. Beautify Meaning: Add styling container
            string styledMeaning = $@"
                <div style='text-align: left; font-size: 20px;'>
                    {meaning}
                </div>";

            // 3. Construct Note
            var note = new
            {
                deckName = deck,
                modelName = "Basic",
                fields = new
                {
                    Front = expression,
                    Back = $"{reading}<br><hr>{styledMeaning}<br><br><div style='font-size: 0.8em; color: #888;'>{highlightSentence}</div>"
                },
                tags = new[] { "VN_Mining" },
                options = new { allowDuplicate = true, duplicateScope = "deck" }
            };

            var payload = new { action = "addNote", version = 6, @params = new { note = note } };

            try
            {
                var json = JsonConvert.SerializeObject(payload);
                var content = new StringContent(json, Encoding.UTF8, "application/json");
                var response = await _client.PostAsync(ANKI_URL, content);
                var resultJson = await response.Content.ReadAsStringAsync();
                dynamic result = JsonConvert.DeserializeObject(resultJson);

                if (result.error != null) return "Anki Error: " + result.error;
                return "Success";
            }
            catch (Exception ex) { return "Error: " + ex.Message; }
        }

        private async Task<dynamic?> Post(string action)
        {
            var json = JsonConvert.SerializeObject(new { action = action, version = 6 });
            var content = new StringContent(json, Encoding.UTF8, "application/json");
            var response = await _client.PostAsync(ANKI_URL, content);
            return JsonConvert.DeserializeObject<dynamic>(await response.Content.ReadAsStringAsync());
        }
    }
}