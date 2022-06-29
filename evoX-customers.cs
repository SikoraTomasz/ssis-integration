
#region Namespaces
using System;
using System.Data;
using Microsoft.SqlServer.Dts.Pipeline.Wrapper;
using Microsoft.SqlServer.Dts.Runtime.Wrapper;
using System.Net;
using Microsoft.SqlServer.Dts.Runtime;
using System.Windows.Forms;
using System.IO;
using System.Web.Script.Serialization;
using System.Text;
#endregion

#region Class
[Microsoft.SqlServer.Dts.Pipeline.SSISScriptComponentEntryPointAttribute]
public class ScriptMain : UserComponent
{
    //WorkGroupMetric[] workGroupMetric;

    public static string Right(string original, int numberCharacters)
    {
        return original.Substring(original.Length - numberCharacters);
    }


    /// <summary>Outputs records to the output buffer</summary>
    public override void CreateNewOutputRows()
    {
        //Get SSIS Variables
        //Set Webservice URL
        string endpoint = "customers";
        string strUrl = Variables.parUrl;
        string endChar = Right(strUrl, 1);
        string wUrl = strUrl + endpoint;
        //if (endChar.Equals("/"))
        //{ wUrl = strUrl + endpoint; }
        //else
        //{ wUrl = strUrl + '/' + endpoint; }

        try
        {
            //Call getWebServiceResult to return our WorkGroupMetric array
            WorkGroupMetric[] outPutMetrics = GetWebServiceResult(wUrl);

            foreach (var metric in outPutMetrics)
            {
                Output0Buffer.AddRow();
                Output0Buffer.id = metric.data.id;
                Output0Buffer.companyname = metric.data.company;
            }

        }
        catch (Exception e)
        {
            FailComponent(e.ToString());
        }

    }

    /// <summary>
    /// Method to return our WorkGroupMetric array
    /// </summary>
    /// <param name="wUrl">The web service URL to call</param>
    /// <returns>An array of WorkGroupMetric composed of the de-serialized JSON</returns>
    private WorkGroupMetric[] GetWebServiceResult(string wUrl)
    {
        string strBearer = "Bearer " + Variables.parBearer;
        ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12;
        //ServicePointManager.ServerCertificateValidationCallback = new System.Net.Security.RemoteCertificateValidationCallback(AcceptAllCertifications);
        HttpWebRequest httpWReq = (HttpWebRequest)WebRequest.Create(wUrl);
        httpWReq.KeepAlive = true;
        httpWReq.Method = "GET";
        httpWReq.Host = "eu.evoapi.io";
        httpWReq.PreAuthenticate = true;
        httpWReq.Headers.Add("Authorization", strBearer);
        httpWReq.Accept = "application/vnd.evolutionx.v1+json";
        //Console.WriteLine(httpWReq.ToString());

        HttpWebResponse httpWResp = (HttpWebResponse)httpWReq.GetResponse();

        WorkGroupMetric[] jsonResponse = null;

        try
        {
            //Test the connection
            if (httpWResp.StatusCode == HttpStatusCode.OK)
            {
                Console.WriteLine("Success");
                Stream responseStream = httpWResp.GetResponseStream();
                string jsonString = null;

                //Set jsonString using a stream reader
                using (StreamReader reader = new StreamReader(responseStream))
                {
                    jsonString = reader.ReadToEnd().Replace("\\\\", "");
                    reader.Close();
                }

                //Deserialize our JSON
                JavaScriptSerializer sr = new JavaScriptSerializer();
                //JSON string comes in with a leading and trailing " that need to be removed for parsing to work correctly
                //The JSON here is serialized weird, normally you would not need this trim
                jsonResponse = sr.Deserialize<WorkGroupMetric[]>(jsonString.Trim('"'));

            }
            //Output connection error message
            else
            {
                FailComponent(httpWResp.StatusCode.ToString());

            }
        }

        //Output JSON parsing error
        catch (Exception e)
        {
            FailComponent(e.ToString());
        }
        return jsonResponse;

    }

    /// <summary>
    /// Outputs error message
    /// </summary>
    /// <param name="errorMsg">Full error text</param>
    private void FailComponent(string errorMsg)
    {
        bool fail = false;
        IDTSComponentMetaData100 compMetadata = this.ComponentMetaData;
        compMetadata.FireError(1, "Error Getting Data From Webservice!", errorMsg, "", 0, out fail);

    }

}

#endregion

#region JSON Class
//Class to hold our work group metrics
public class Datum
{
    public int id { get; set; }
    public string company { get; set; }
    public string admin_email { get; set; }
    public string account_number { get; set; }
    public string phone { get; set; }
    public string fax { get; set; }
    public string logo { get; set; }
    public int parent_id { get; set; }
    public bool enabled { get; set; }
    public string tax_number { get; set; }
    public string company_number { get; set; }
    public string duns_number { get; set; }
    public bool credit_account { get; set; }
    public string seller_reference { get; set; }
    public int points_balance { get; set; }
    public int points_pending { get; set; }
    public int rewards_status { get; set; }
    public List<object> child_account_ids { get; set; }
    public object branch { get; set; }
    public object account_manager { get; set; }
    public List<object> account_manager_list { get; set; }
    public DefaultBillingAddress default_billing_address { get; set; }
    public DefaultShippingAddress default_shipping_address { get; set; }
    public Pricing pricing { get; set; }
    public Tax tax { get; set; }
    public Shipping shipping { get; set; }
    public List<object> labels { get; set; }
    public int created_at { get; set; }
    public int updated_at { get; set; }
}

public class DefaultBillingAddress
{
    public int id { get; set; }
    public string title { get; set; }
    public string code { get; set; }
    public string name { get; set; }
    public string company { get; set; }
    public string phone { get; set; }
    public string line_1 { get; set; }
    public string line_2 { get; set; }
    public string line_3 { get; set; }
    public string city { get; set; }
    public string state { get; set; }
    public string zip { get; set; }
    public string country { get; set; }
    public string note { get; set; }
    public string seller_reference { get; set; }
}

public class DefaultShippingAddress
{
    public int id { get; set; }
    public string title { get; set; }
    public string code { get; set; }
    public string name { get; set; }
    public string company { get; set; }
    public string phone { get; set; }
    public string line_1 { get; set; }
    public string line_2 { get; set; }
    public string line_3 { get; set; }
    public string city { get; set; }
    public string state { get; set; }
    public string zip { get; set; }
    public string country { get; set; }
    public string note { get; set; }
    public string seller_reference { get; set; }
}

public class Pricing
{
    public bool pricing_module { get; set; }
    public string pricing_email { get; set; }
    public bool show_cheapest_price { get; set; }
    public bool exclude_global_contract { get; set; }
    public bool pricing_override_addtocart { get; set; }
}

public class WorkGroupMetric
{

    public string status { get; set; }
    public bool has_more { get; set; }
    public int last_id { get; set; }
    public Datum data = new Datum();
    //public List<Datum> data { get; set; }
}

public class Shipping
{
    public bool @override { get; set; }
    public int override_amount { get; set; }
}

public class Tax
{
    public bool @override { get; set; }
    public int override_amount { get; set; }
    public bool override_shipping { get; set; }
    public int override_shipping_amount { get; set; }
}
#endregion
