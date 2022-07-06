
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
using System.Collections.Generic;
using System.Text;
using System.Collections;

#endregion

#region Class
[Microsoft.SqlServer.Dts.Pipeline.SSISScriptComponentEntryPointAttribute]
public class ScriptMain : UserComponent
{


    public static string Right(string original, int numberCharacters)
    {
        return original.Substring(original.Length - numberCharacters);
    }

    /// <summary>Outputs records to the output buffer</summary>
    public override void CreateNewOutputRows()
    {
        // Get SSIS Variables
        // Set Webservice URL
        string strLastID = Variables.varLastID.ToString();

        string endpoint = "customers?limit=100&from_id=" + strLastID;
        string strUrl = Variables.parUrl;
        string endChar = Right(strUrl, 1);
        string wUrl = strUrl + endpoint;

        try
        {
            // Call getWebServiceResult to return our WorkGroupMetric array
            Root[] outPutMetrics = GetWebServiceResult(wUrl);

            foreach (var metric in outPutMetrics)
            {
                Output0Buffer.AddRow();
                Output0Buffer.id = metric.id;
                Output0Buffer.company = metric.company;
                Output0Buffer.adminemail = metric.admin_email;
                Output0Buffer.accountnumber = metric.account_number;
                Output0Buffer.phone = metric.phone;
                Output0Buffer.fax = metric.fax;
                Output0Buffer.parentid = metric.parent_id;
                Output0Buffer.taxnumber = metric.tax_number;
                Output0Buffer.enabled = metric.enabled;
                Output0Buffer.companynumber = metric.company_number;
                Output0Buffer.dunsnumber = metric.duns_number;
                Output0Buffer.creditaccount = metric.credit_account;
                Output0Buffer.sellerreference = metric.seller_reference;

                if (metric.default_billing_address != null)
                {
                    Output0Buffer.defaultbillingaddresscity = metric.default_billing_address.city;
                    Output0Buffer.defaultbillingaddresscompany = metric.default_billing_address.company;
                    Output0Buffer.defaultbillingaddresscountry = metric.default_billing_address.country;
                    Output0Buffer.defaultbillingaddressname = metric.default_billing_address.name;
                    Output0Buffer.defaultbillingaddressphone = metric.default_billing_address.phone;
                    Output0Buffer.defaultbillingaddressstate = metric.default_billing_address.state;

                };

                if (metric.default_shipping_address != null)
                {
                    Output0Buffer.defaultshippingaddresscity = metric.default_shipping_address.city;
                    Output0Buffer.defaultshippingaddresscompany = metric.default_shipping_address.company;
                    Output0Buffer.defaultshippingaddresscountry = metric.default_shipping_address.country;
                    Output0Buffer.defaultshippingaddressname = metric.default_shipping_address.name;
                    Output0Buffer.defaultshippingaddressphone = metric.default_shipping_address.phone;
                    Output0Buffer.defaultshippingaddressstate = metric.default_shipping_address.state;

                };
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
    /// <returns>An array of WorkGroupMetric composed of the de-serialized
    /// JSON</returns>
    private Root[] GetWebServiceResult(string wUrl)
    {
        string strBearer = "Bearer " + Variables.parBearer;
        ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12;
        HttpWebRequest httpWReq = (HttpWebRequest)WebRequest.Create(wUrl);
        httpWReq.KeepAlive = true;
        httpWReq.Method = "GET";
        httpWReq.Host = "eu.evoapi.io";
        httpWReq.PreAuthenticate = true;
        httpWReq.Headers.Add("Authorization", strBearer);
        httpWReq.Accept = "application/vnd.evolutionx.v1+json";

        HttpWebResponse httpWResp = (HttpWebResponse)httpWReq.GetResponse();

        Root[] jsonResponse = null;

        try
        {
            // Test the connection
            if (httpWResp.StatusCode == HttpStatusCode.OK)
            {
                Console.WriteLine("Success");
                Stream responseStream = httpWResp.GetResponseStream();
                string jsonString = null;

                // Set jsonString using a stream reader
                using (StreamReader reader = new StreamReader(responseStream))
                {
                    
                    jsonString = reader.ReadToEnd().Replace("\\\\", "");
                    reader.Close();
                }

                int found = 0;
                found = jsonString.IndexOf("[");
                string jsonCleansed = "[" + jsonString.Substring(found + 1).TrimEnd('}').TrimEnd(']') + "]";

                // Deserialize our JSON
                JavaScriptSerializer sr = new JavaScriptSerializer();
                // JSON string comes in with a leading and trailing " that need to be
                // removed for parsing to work correctly The JSON here is serialized
                // weird, normally you would not need this trim
                jsonResponse = sr.Deserialize<Root[]>(jsonCleansed.Trim('"'));

            }
            // Output connection error message
            else
            {
                FailComponent(httpWResp.StatusCode.ToString());
            }
        }

        // Output JSON parsing error
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
        compMetadata.FireError(1, "Error Getting Data From Webservice!", errorMsg,
                               "", 0, out fail);
    }
}

#endregion

#region JSON Class
// Class to hold our work group metrics

public class Root
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
    public object branch { get; set; }
    public object account_manager { get; set; }
    public DefaultBillingAddress default_billing_address { get; set; }
    public DefaultShippingAddress default_shipping_address { get; set; }
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
#endregion
