using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Lucene.Net.Documents;

namespace ActiveLucene.Net.FieldHandler
{
    public class FloatFieldHandlerContext : FieldHandlerContextBase<float>
    {
        private NumericField _field;

        public override void Init()
        {
            _field = new NumericField(Configuration.Name, Configuration.Store, Configuration.Index != Field.Index.NO);
        }

        public override float GetValue(Document document)
        {
            return IfNotNull(document.Get(Configuration.Name), float.Parse);
        }

        public override void SetFields(Document document, float value)
        {
            document.Add(_field.SetFloatValue(value));
        }
    }
}
