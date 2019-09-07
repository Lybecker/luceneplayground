using System;
using System.Collections.Generic;
using System.Linq;
using Lucene.Net.Index;

namespace Com.Lybecker.LuceneLibrary
{
    public class Util
    {
        public void PrintTerms(IndexReader indexReader, string fieldName, int docFreqThreshold = 0)
        {
            Console.WriteLine("Terms for field \"{0}\" in directory {1} with DocFreq >= {2}", fieldName, indexReader.Directory(), docFreqThreshold);

            GetTerms(indexReader, fieldName, docFreqThreshold)
                .OrderByDescending(x => x.DocFreq)
                .ThenByDescending(x => x.FieldName)
                .ToList()
                .ForEach(Console.WriteLine);
        }

        public IEnumerable<LuceneTerm> GetTerms(IndexReader indexReader, string fieldName, int docFreqThreshold = 0, int minTermLength = 0)
        {
            if (!indexReader.GetFieldNames(IndexReader.FieldOption.ALL).Contains(fieldName))
                throw new ArgumentException("fieldName not found in index");

            var luceneTerms = new List<LuceneTerm>();

            bool foundField = false;

            var terms = indexReader.Terms();
            while (terms.Next())
            {
                Term term = terms.Term;
                string termText = term.Text;
                int docFreq = terms.DocFreq();

                if (term.Field.Equals(fieldName, StringComparison.Ordinal))
                {
                    if (docFreq >= docFreqThreshold && termText.Length >= minTermLength)
                    {
                    luceneTerms.Add(new LuceneTerm { FieldName = fieldName, Term = termText, DocFreq = docFreq });
                    foundField = true;
                    }
                }
                else
                {
                    if (foundField)
                        break;
                }
            }

            return luceneTerms;
        }

        public IEnumerable<string> GetDocumentFieldValues(IndexReader indexReader, string fieldName)
        {
            if (!indexReader.GetFieldNames(IndexReader.FieldOption.ALL).Contains(fieldName))
                throw new ArgumentException("fieldName not found in index");

            var fieldValues = new List<string>(indexReader.NumDocs());

            for (int i = 0; i < indexReader.MaxDoc; i++)
            {
                if (!indexReader.IsDeleted(i))
                    fieldValues.Add(indexReader.Document(i).Get(fieldName));
            }

            return fieldValues;
        }
    }
}