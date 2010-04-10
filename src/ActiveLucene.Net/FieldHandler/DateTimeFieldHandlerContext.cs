using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Lucene.Net.Documents;

namespace ActiveLucene.Net.FieldHandler
{
    public class DateTimeFieldHandlerContext : FieldHandlerContextBase<DateTime>
    {
        private Field _field;

        public override void Init()
        {
            _field = new Field(Configuration.Name, "", Configuration.Store, Configuration.Index);
        }

        public override DateTime GetValue(Document document)
        {
            return IfNotNull(document.Get(Configuration.Name), DateTools.StringToDate);
        }

        public override void SetFields(Document document, DateTime value)
        {
            document.Add(_field.Set(DateTools.DateToString(value, Configuration.DateResolution)));
        }
    }
}
