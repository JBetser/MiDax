using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Services;
using MidaxWebService;
using System.Collections;
using System.Web.Script.Services;

[WebService(Namespace = "https://bitlsoft.com/")]
[WebServiceBinding(ConformsTo = WsiProfiles.BasicProfile1_1)]

[System.Web.Script.Services.ScriptService]
public class MidaxWS : System.Web.Services.WebService {

    public MidaxWS()
    {
    }
    
    [ScriptMethod(ResponseFormat = ResponseFormat.Json)]
    [WebMethod(EnableSession = true)]
    public string GetStockData(string begin, string end, string stockid)
    {
        return MidaxWebServer.Instance.GetStockData(begin, end, stockid);
    }

    [ScriptMethod(ResponseFormat = ResponseFormat.Json)]
    [WebMethod(EnableSession = true)]
    public string GetIndicatorData(string begin, string end, string indicatorid)
    {
        return MidaxWebServer.Instance.GetIndicatorData(begin, end, indicatorid);
    }

    [ScriptMethod(ResponseFormat = ResponseFormat.Json)]
    [WebMethod(EnableSession = true)]
    public string GetSignalData(string begin, string end, string signalid)
    {
        return MidaxWebServer.Instance.GetSignalData(begin, end, signalid);
    }
}

