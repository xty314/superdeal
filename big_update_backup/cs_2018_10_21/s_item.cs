<%@ Import Namespace="System.Xml.Serialization" %>
<%@ Import Namespace="System.Web.Services.Protocols" %>
<%@ Import Namespace="System.ComponentModel" %>
<%@ Import Namespace="System.Web.Services" %>

<script language=C# runat=server>

[System.Diagnostics.DebuggerStepThroughAttribute()]
[System.ComponentModel.DesignerCategoryAttribute("code")]
[System.Web.Services.WebServiceBindingAttribute(Name="CItemSoap", Namespace="http://eznz.com/")]
class csItem : System.Web.Services.Protocols.SoapHttpClientProtocol
{
    public csItem(string Url)
    {
        this.Url = Url;
    }

    [System.Web.Services.Protocols.SoapDocumentMethodAttribute("http://eznz.com/GetItemDetail", RequestNamespace="http://eznz.com", ResponseNamespace="http://eznz.com", Use=System.Web.Services.Description.SoapBindingUse.Literal, ParameterStyle=System.Web.Services.Protocols.SoapParameterStyle.Wrapped)]
    public DataSet GetItemDetail(string code)
    {
        object[] results = this.Invoke("GetItemDetail", new object[]{code});
        return ((DataSet)(results[0]));
    }

    [System.Web.Services.Protocols.SoapDocumentMethodAttribute("http://eznz.com/GetItemPhotoType", RequestNamespace="http://eznz.com", ResponseNamespace="http://eznz.com", Use=System.Web.Services.Description.SoapBindingUse.Literal, ParameterStyle=System.Web.Services.Protocols.SoapParameterStyle.Wrapped)]
    public string GetItemPhotoType(string code)
    {
        object[] results = this.Invoke("GetItemPhotoType", new object[]{code});
        return ((string)(results[0]));
    }

    [System.Web.Services.Protocols.SoapDocumentMethodAttribute("http://eznz.com/GetItemPhotoData", RequestNamespace="http://eznz.com", ResponseNamespace="http://eznz.com", Use=System.Web.Services.Description.SoapBindingUse.Literal, ParameterStyle=System.Web.Services.Protocols.SoapParameterStyle.Wrapped)]
    public byte[] GetItemPhotoData(string fileNameWithoutPath)
    {
        object[] results = this.Invoke("GetItemPhotoData", new object[]{fileNameWithoutPath});
        return ((byte[])(results[0]));
    }
}
</script>