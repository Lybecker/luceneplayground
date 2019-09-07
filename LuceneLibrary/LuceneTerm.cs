namespace Com.Lybecker.LuceneLibrary
{
    public class LuceneTerm
    {
        public string FieldName { get; set; }
        public string Term { get; set; }
        public int DocFreq { get; set; }

        public override string ToString()
        {
            return string.Format("{0}:{1} DocFreq {2}", FieldName, Term, DocFreq);
        }
    }
}