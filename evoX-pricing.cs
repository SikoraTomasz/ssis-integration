
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

        string endpoint = "pricing?limit=100&from_id=" + strLastID;
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
                Output0Buffer.name = metric.name;
                Output0Buffer.typeid = metric.type_id;
                Output0Buffer.priority = metric.priority;
                Output0Buffer.bandpriority = metric.band_priority;
                Output0Buffer.itempriority = metric.item_priority;
                Output0Buffer.categorypriority = metric.category_priority;
                Output0Buffer.brandpriority = metric.brand_priority;
                Output0Buffer.mailerpriority = metric.mailer_priority;
                Output0Buffer.quantitypriority = metric.quantity_priority;
                Output0Buffer.accounts = metric.accounts;
                Output0Buffer.users = metric.users;
                Output0Buffer.items = metric.items;
                Output0Buffer.bands = metric.bands;
                Output0Buffer.categories = metric.categories;
                Output0Buffer.brands = metric.brands;
                Output0Buffer.visible = metric.visible;
                Output0Buffer.cheapest = metric.cheapest;
                Output0Buffer.autodeletion = metric.auto_deletion;
                Output0Buffer.from = metric.from;
                Output0Buffer.to = metric.to;
                Output0Buffer.enabled = metric.enabled;
                Output0Buffer.createdat = metric.created_at;
                Output0Buffer.updatedat = metric.updated_at;
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
    public string name { get; set; }
    public int type_id { get; set; }
    public int priority { get; set; }
    public int band_priority { get; set; }
    public int item_priority { get; set; }
    public int category_priority { get; set; }
    public int brand_priority { get; set; }
    public int mailer_priority { get; set; }
    public int quantity_priority { get; set; }
    public int accounts { get; set; }
    public int users { get; set; }
    public int items { get; set; }
    public int bands { get; set; }
    public int categories { get; set; }
    public int brands { get; set; }
    public bool visible { get; set; }
    public bool cheapest { get; set; }
    public bool auto_deletion { get; set; }
    public int from { get; set; }
    public int to { get; set; }
    public bool enabled { get; set; }
    public int created_at { get; set; }
    public int updated_at { get; set; }
}
#endregion
