
<%@Import Namespace="System" %>
<%@Import Namespace="System.Data" %>
<%@Import Namespace="System.Configuration" %>
<%@Import Namespace="System.Web" %>
<%@Import Namespace="System.Web.Security" %>
<%@Import Namespace="System.Web.UI" %>
<%@Import Namespace="System.Web.UI.HtmlControls" %>
<%@Import Namespace="System.Web.UI.WebControls" %>
<%@Import Namespace="System.Web.UI.WebControls.WebParts" %>
<%@Import Namespace="System.IO" %>
<%@Import Namespace="System.Reflection" %>
<%@Import Namespace="System.Xml" %>
<%@Import Namespace="System.Net" %>

<script language=C# runat=server>


        /// <summary>
        /// Main class for submitting transactions via PxPay using static methods
        /// </summary>
        public class PxPay
        {
            private string _WebServiceUrl = ConfigurationManager.AppSettings["PaymentExpress.PxPay"];
            private string _PxPayUserId;
            private string _PxPayKey;

            /// <summary>
            /// 
            /// </summary>
            /// <param name="PxPayUserId"></param>
            /// <param name="PxPayKey"></param>
            public PxPay(string PxPayUserId, string PxPayKey)
            {
                _PxPayUserId = PxPayUserId;
                _PxPayKey = PxPayKey;
            }

/// <summary>
/// 
/// </summary>
/// <param name="result"></param>
/// <returns></returns>
            public ResponseOutput ProcessResponse(string result)
            {
                ResponseOutput myResult = new ResponseOutput(SubmitXml(ProcessResponseXml(result)));
                return myResult;
            }

            /// <summary>
            /// 
            /// </summary>
            /// <param name="input"></param>
            /// <returns></returns>
            public RequestOutput GenerateRequest(RequestInput input)
            {
                RequestOutput result = new RequestOutput(SubmitXml(GenerateRequestXml(input)));
                return result;
            }

            private string SubmitXml(string InputXml)
            {
                HttpWebRequest webReq = (HttpWebRequest)WebRequest.Create(_WebServiceUrl);
                webReq.Method = "POST";

                byte[] reqBytes;

                reqBytes = System.Text.Encoding.UTF8.GetBytes(InputXml);
                webReq.ContentType = "application/x-www-form-urlencoded";
                webReq.ContentLength = reqBytes.Length;
                webReq.Timeout = 5000;
                Stream requestStream = webReq.GetRequestStream();
                requestStream.Write(reqBytes, 0, reqBytes.Length);
                requestStream.Close();

                HttpWebResponse webResponse = (HttpWebResponse)webReq.GetResponse();
                using (StreamReader sr = new StreamReader(webResponse.GetResponseStream(), System.Text.Encoding.ASCII))
                {
                    return sr.ReadToEnd();
                }
            }

            /// <summary>
            /// Generates the XML required for a GenerateRequest call
            /// </summary>
            /// <param name="input"></param>
            /// <returns></returns>
            private string GenerateRequestXml(RequestInput input)
            {

                StringWriter sw = new StringWriter();

                XmlWriterSettings settings = new XmlWriterSettings();
                settings.Indent = true;
                settings.NewLineOnAttributes = false;
                settings.OmitXmlDeclaration = true;

                using (XmlWriter writer = XmlWriter.Create(sw, settings))
                {
                    writer.WriteStartDocument();
                    writer.WriteStartElement("GenerateRequest");
                    writer.WriteElementString("PxPayUserId", _PxPayUserId);
                    writer.WriteElementString("PxPayKey", _PxPayKey);

                    PropertyInfo[] properties = input.GetType().GetProperties();

                    foreach (PropertyInfo prop in properties)
                    {
                        if (prop.CanWrite)
                        {
                            string val = (string)prop.GetValue(input, null);

                            if (val != null || val != string.Empty)
                            {

                                writer.WriteElementString(prop.Name, val);
                            }
                        }
                    }
                    writer.WriteEndElement();
                    writer.WriteEndDocument();
                    writer.Flush();
                }

                return sw.ToString();
            }

            /// <summary>
            /// Generates the XML required for a ProcessResponse call
            /// </summary>
            /// <param name="result"></param>
            /// <returns></returns>
            private string ProcessResponseXml(string result)
            {

                StringWriter sw = new StringWriter();

                XmlWriterSettings settings = new XmlWriterSettings();
                settings.Indent = true;
                settings.NewLineOnAttributes = false;
                settings.OmitXmlDeclaration = true;

                using (XmlWriter writer = XmlWriter.Create(sw, settings))
                {
                    writer.WriteStartDocument();
                    writer.WriteStartElement("ProcessResponse");
                    writer.WriteElementString("PxPayUserId", _PxPayUserId);
                    writer.WriteElementString("PxPayKey", _PxPayKey);
                    writer.WriteElementString("Response", result);
                    writer.WriteEndElement();
                    writer.WriteEndDocument();
                    writer.Flush();
                }

                return sw.ToString();
            }

        }
        

        /// <summary>
        /// Class containing properties describing transaction details
        /// </summary>
        public class RequestInput
        {
            private string _AmountInput;
            private string _BillingId;
            private string _CurrencyInput;
            private string _DpsBillingId;
            private string _DpsTxnRef;
            private string _EmailAddress;
            private string _EnableAddBillCard;
            private string _MerchantReference;
            private string _TxnData1;
            private string _TxnData2;
            private string _TxnData3;
            private string _TxnType;
            private string _TxnId;
            private string _UrlFail;
            private string _UrlSuccess;
            private string _Opt;


            public RequestInput()
            {
            }


            public string AmountInput
            {
                get
                {
                    return _AmountInput;
                }
                set
                {
                    _AmountInput = value;
                }
            }

            public string BillingId
            {
                get
                {
                    return _BillingId;
                }
                set
                {
                    _BillingId = value;
                }
            }

            public string CurrencyInput
            {
                get
                {
                    return _CurrencyInput;
                }
                set
                {
                    _CurrencyInput = value;
                }
            }

            public string DpsBillingId
            {
                get
                {
                    return _DpsBillingId;
                }
                set
                {
                    _DpsBillingId = value;
                }
            }

            public string DpsTxnRef
            {
                get
                {
                    return _DpsTxnRef;
                }
                set
                {
                    _DpsTxnRef = value;
                }
            }

            public string EmailAddress
            {
                get
                {
                    return _EmailAddress;
                }
                set
                {
                    _EmailAddress = value;
                }
            }

            public string EnableAddBillCard
            {
                get
                {
                    return _EnableAddBillCard;
                }
                set
                {
                    _EnableAddBillCard = value;
                }
            }

            public string MerchantReference
            {
                get
                {
                    return _MerchantReference;
                }
                set
                {
                    _MerchantReference = value;
                }
            }

            public string TxnData1
            {
                get
                {
                    return _TxnData1;
                }
                set
                {
                    _TxnData1 = value;
                }
            }

            public string TxnData2
            {
                get
                {
                    return _TxnData2;
                }
                set
                {
                    _TxnData2 = value;
                }
            }

            public string TxnData3
            {
                get
                {
                    return _TxnData3;
                }
                set
                {
                    _TxnData3 = value;
                }
            }

            public string TxnType
            {
                get
                {
                    return _TxnType;
                }
                set
                {
                    _TxnType = value;
                }
            }

            public string TxnId
            {
                get
                {
                    return _TxnId;
                }
                set
                {
                    _TxnId = value;
                }
            }

            public string UrlFail
            {
                get
                {
                    return _UrlFail;
                }
                set
                {
                    _UrlFail = value;
                }
            }

            public string UrlSuccess
            {
                get
                {
                    return _UrlSuccess;
                }
                set
                {
                    _UrlSuccess = value;
                }
            }

            public string Opt
            {
                get
                {
                    return _Opt;
                }
                set
                {
                    _Opt = value;
                }
            }

            // If there are any additional input parameters simply add a new read/write property

        }

        /// <summary>
        /// Class containing properties describing the output of the request
        /// </summary>
        public class RequestOutput
        {

            public RequestOutput(string Xml)
            {
                _Xml = Xml;
                SetProperty();
            }

            private string _valid;
            private string _URI;

            private string _Xml;

            public string valid
            {
                get
                {
                    return _valid;
                }
                set
                {
                    _valid = value;
                }
            }

            public string URI
            {
                get
                {
                    return _URI;
                }
                set
                {
                    _URI = value;
                }
            }

            public string Url
            {
                get
                {
                    return _URI.Replace("&amp;", "&");
                }

            }

            private void SetProperty()
            {

                XmlReader reader = XmlReader.Create(new StringReader(_Xml));

                while (reader.Read())
                {
                    PropertyInfo prop;
                    if (reader.NodeType == XmlNodeType.Element)
                    {
                        prop = this.GetType().GetProperty(reader.Name);
                        if (prop != null)
                        {
                            this.GetType().GetProperty(reader.Name).SetValue(this, reader.ReadString(), System.Reflection.BindingFlags.Default, null, null, null);
                        }
                        if (reader.HasAttributes)
                        {

                            for (int count = 0; count < reader.AttributeCount; count++)
                            {
                                //Read the current attribute
                                reader.MoveToAttribute(count);
                                prop = this.GetType().GetProperty(reader.Name);
                                if (prop != null)
                                {
                                    this.GetType().GetProperty(reader.Name).SetValue(this, reader.Value, System.Reflection.BindingFlags.Default, null, null, null);
                                }
                            }
                        }
                    }
                }

            }
        }

        /// <summary>
        /// Class containing properties describing the outcome of the transaction
        /// </summary>
        public class ResponseOutput
        {

            public ResponseOutput(string Xml)
            {
                _Xml = Xml;
                SetProperty();
            }

            private string _valid;
            private string _AmountSettlement;
            private string _AuthCode;
            private string _CardName;
            private string _CardNumber;
            private string _DateExpiry;
            private string _DpsTxnRef;
            private string _Success;
            private string _ResponseText;
            private string _DpsBillingId;
            private string _CardHolderName;
            private string _CurrencySettlement;
            private string _TxnData1;
            private string _TxnData2;
            private string _TxnData3;
            private string _TxnType;
            private string _CurrencyInput;
            private string _MerchantReference;
            private string _ClientInfo;
            private string _TxnId;
            private string _EmailAddress;
            private string _BillingId;
            private string _TxnMac;

            private string _Xml;

            public string valid
            {
                get
                {
                    return _valid;
                }
                set
                {
                    _valid = value;
                }
            }

            public string AmountSettlement
            {
                get
                {
                    return _AmountSettlement;
                }
                set
                {
                    _AmountSettlement = value;
                }
            }

            public string AuthCode
            {
                get
                {
                    return _AuthCode;
                }
                set
                {
                    _AuthCode = value;
                }
            }

            public string CardName
            {
                get
                {
                    return _CardName;
                }
                set
                {
                    _CardName = value;
                }
            }

            public string CardNumber
            {
                get
                {
                    return _CardNumber;
                }
                set
                {
                    _CardNumber = value;
                }
            }

            public string DateExpiry
            {
                get
                {
                    return _DateExpiry;
                }
                set
                {
                    _DateExpiry = value;
                }
            }

            public string DpsTxnRef
            {
                get
                {
                    return _DpsTxnRef;
                }
                set
                {
                    _DpsTxnRef = value;
                }
            }

            public string Success
            {
                get
                {
                    return _Success;
                }
                set
                {
                    _Success = value;
                }
            }

            public string ResponseText
            {
                get
                {
                    return _ResponseText;
                }
                set
                {
                    _ResponseText = value;
                }
            }

            public string DpsBillingId
            {
                get
                {
                    return _DpsBillingId;
                }
                set
                {
                    _DpsBillingId = value;
                }
            }

            public string CardHolderName
            {
                get
                {
                    return _CardHolderName;
                }
                set
                {
                    _CardHolderName = value;
                }
            }

            public string CurrencySettlement
            {
                get
                {
                    return _CurrencySettlement;
                }
                set
                {
                    _CurrencySettlement = value;
                }
            }

            public string TxnData1
            {
                get
                {
                    return _TxnData1;
                }
                set
                {
                    _TxnData1 = value;
                }
            }

            public string TxnData2
            {
                get
                {
                    return _TxnData2;
                }
                set
                {
                    _TxnData2 = value;
                }
            }

            public string TxnData3
            {
                get
                {
                    return _TxnData3;
                }
                set
                {
                    _TxnData3 = value;
                }
            }

            public string TxnType
            {
                get
                {
                    return _TxnType;
                }
                set
                {
                    _TxnType = value;
                }
            }

            public string CurrencyInput
            {
                get
                {
                    return _CurrencyInput;
                }
                set
                {
                    _CurrencyInput = value;
                }
            }


            public string MerchantReference
            {
                get
                {
                    return _MerchantReference;
                }
                set
                {
                    _MerchantReference = value;
                }
            }

            public string ClientInfo
            {
                get
                {
                    return _ClientInfo;
                }
                set
                {
                    _ClientInfo = value;
                }
            }

            public string TxnId
            {
                get
                {
                    return _TxnId;
                }
                set
                {
                    _TxnId = value;
                }
            }

            public string EmailAddress
            {
                get
                {
                    return _EmailAddress;
                }
                set
                {
                    _EmailAddress = value;
                }
            }

            public string BillingId
            {
                get
                {
                    return _BillingId;
                }
                set
                {
                    _BillingId = value;
                }
            }

            public string TxnMac
            {
                get
                {
                    return _TxnMac;
                }
                set
                {
                    _TxnMac = value;
                }
            }

            // If there are any additional elements or attributes added to the output XML simply add a property of the same name.

            private void SetProperty()
            {

                XmlReader reader = XmlReader.Create(new StringReader(_Xml));

                while (reader.Read())
                {
                    PropertyInfo prop;
                    if (reader.NodeType == XmlNodeType.Element)
                    {
                        prop = this.GetType().GetProperty(reader.Name);
                        if (prop != null)
                        {
                            this.GetType().GetProperty(reader.Name).SetValue(this, reader.ReadString(), System.Reflection.BindingFlags.Default, null, null, null);
                        }
                        if (reader.HasAttributes)
                        {

                            for (int count = 0; count < reader.AttributeCount; count++)
                            {
                                //Read the current attribute
                                reader.MoveToAttribute(count);
                                prop = this.GetType().GetProperty(reader.Name);
                                if (prop != null)
                                {
                                    this.GetType().GetProperty(reader.Name).SetValue(this, reader.Value, System.Reflection.BindingFlags.Default, null, null, null);
                                }
                            }
                        }
                    }
                }

            }


        }

    
    

</script>