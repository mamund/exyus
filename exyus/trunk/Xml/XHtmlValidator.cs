using System;
using System.Collections.ObjectModel;
using System.Text;
using System.IO;
using System.Xml;
using System.Xml.Schema;

namespace Exyus.Xml
{
    public class XHtmlValidator
    {
        private Collection<ValidationRecord> _records = new Collection<ValidationRecord>();
        private string _document;

        public XHtmlValidator(string input)
        {
            _document = input;
        }

        public Collection<ValidationRecord> Validate()
        {
            return Validate(ValidationType.Auto);
        }
        public Collection<ValidationRecord> Validate(ValidationType  vtype)
        {
            XmlReaderSettings xrs = new XmlReaderSettings();
            xrs.ProhibitDtd = false;
            xrs.ValidationType = vtype;
            xrs.ValidationEventHandler += new ValidationEventHandler(xrs_ValidationEventHandler);
            xrs.XmlResolver = new XmlUrlResolver();
            using (StringReader sr = new StringReader(_document))
            using (XmlReader xr = XmlReader.Create(sr, xrs))
            {
                try
                {
                    while (xr.Read()) ;
                }
                catch (XmlException xmlExc)
                {
                    _records.Add(new ValidationRecord(xmlExc));
                }
            }

            return _records;
        }

        private void xrs_ValidationEventHandler(object sender, ValidationEventArgs e)
        {
            _records.Add(new ValidationRecord(e));
        }
    }

    public class ValidationRecord
    {
        public ValidationRecord(ValidationEventArgs args)
        {
            LineNumber = args.Exception.LineNumber;
            LinePosition = args.Exception.LinePosition;
            Message = args.Message;
            Severity = args.Severity;
        }

        public ValidationRecord(XmlException xmlExc)
        {
            LineNumber = xmlExc.LineNumber;
            LinePosition = xmlExc.LinePosition;
            Message = xmlExc.Message;
            Severity = XmlSeverityType.Error;
        }

        public int LineNumber;
        public int LinePosition;
        public string Message;
        public XmlSeverityType Severity;

        public override string ToString()
        {
            return String.Format("{0}: {1} (Line:{2}, Position:{3})"
                , Severity, Message, LineNumber, LinePosition);
        }
    }
}
