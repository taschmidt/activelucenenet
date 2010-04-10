using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Lucene.Net.Documents;

namespace ActiveLucene.Net.FieldHandler
{
    public class GenericFieldHandlerContext<T> : FieldHandlerContextBase<T>
    {
        private Field _field;

        public override void Init()
        {
            _field = new Field(Configuration.Name, "", Configuration.Store, Configuration.Index);
        }

        public override T GetValue(Document document)
        {
            return IfNotNull(document.Get(Configuration.Name), str => (T) Convert.ChangeType(str, typeof (T)));
        }

        public override void SetFields(Document document, T value)
        {
            document.Add(_field.Set(value.ToString()));
        }
    }
}
