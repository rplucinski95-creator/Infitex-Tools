using System;

namespace InfitexTools.Models
{
    public class RevisionHistoryItem
    {
        public string Revision { get; set; }
        public DateTime Date { get; set; }
        public string Author { get; set; }
        public string Comment { get; set; }
    }
}