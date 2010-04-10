using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Lucene.Net.Documents;

namespace ActiveLucene.Net.FieldHandler
{
    public class DoubleFieldHandlerContext : FieldHandlerContextBase<double>
    {
        private NumericField _field;

        public override void Init()
        {
            _field = new NumericField(Configuration.Name, Configuration.Store, Configuration.Index != Field.Index.NO);
        }

        public override double GetValue(Document document)
        {
            return IfNotNull(document.Get(Configuration.Name), double.Parse);
        }

        public override void SetFields(Document document, double value)
        {
            document.Add(_field.SetDoubleValue(value));
        }
    }
}
