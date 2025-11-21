namespace OverlayApp.Models
{
    public class Token
    {
        public string Surface { get; set; } = "";       // displayed word
        public string OriginalForm { get; set; } = "";  // dictionary form
        public string Reading { get; set; } = "";
        public string PartOfSpeech { get; set; } = "";
        public bool IsWord { get; set; } = true;
    }
}