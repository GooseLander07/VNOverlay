using System.Collections.Generic;
using System.Windows.Documents;
using System.Windows.Media;

namespace OverlayApp.Models
{
    public class DictionaryEntry
    {
        public string Headword { get; set; } = "";
        public string Reading { get; set; } = "";
        public int Score { get; set; } = 0;
        public bool IsPriority => Score >= 1000;

        public FlowDocument DefinitionDocument { get; set; } = CreateEmptyDoc();

        public List<string> Tags { get; set; } = new List<string>();
        public List<Sense> Senses { get; set; } = new List<Sense>();

        private static FlowDocument CreateEmptyDoc()
        {
            return new FlowDocument(new Paragraph(new Run("Loading...")))
            {
                PagePadding = new System.Windows.Thickness(0),
                Background = Brushes.Transparent
            };
        }
    }

    public class Sense
    {
        public List<string> PoSTags { get; set; } = new List<string>();
        public List<string> Glossaries { get; set; } = new List<string>();
        public List<string> Info { get; set; } = new List<string>();
        public List<ExampleSentence> Examples { get; set; } = new List<ExampleSentence>();
    }

    public class ExampleSentence
    {
        public string Japanese { get; set; } = "";
        public string English { get; set; } = "";
    }
}