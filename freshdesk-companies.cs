#region Namespaces
using System;
using System.Data;
using Microsoft.SqlServer.Dts.Pipeline.Wrapper;
using Microsoft.SqlServer.Dts.Runtime;
using System.Net;
using System.Windows.Forms;
using System.IO;
using System.Web.Script.Serialization;
using System.Collections.Generic;
#endregion

#region JSON Class
//Class to hold our work group metrics
// Root myDeserializedClass = JsonConvert.DeserializeObject<Root>(myJsonResponse);
public class CustomFields
{
    public string specific_req { get; set; }
}

public class Root
{
    public string id { get; set; }
    public string name { get; set; }
    public string description { get; set; }
    public string note { get; set; }
    public List<string> domains { get; set; }
    public DateTime? created_at { get; set; }
    public DateTime? updated_at { get; set; }
    public CustomFields custom_fields { get; set; }
}


#endregion

#region Class
[Microsoft.SqlServer.Dts.Pipeline.SSISScriptComponentEntryPointAttribute]
public class ScriptMain : UserComponent
{
    Root[] root;

    public static bool IsEmpty<T>(List<T> list)
    {
        if (list == null)
        {
            return true;
        }

        return list.Count == 0;
    }
    /// <summary>
    /// Method to return our WorkGroupMetric array
    /// </summary>
    /// <param name="wUrl">The web service URL to call</param>
    /// <returns>An array of WorkGroupMetric composed of the de-serialized JSON</returns>
    private Root[] GetWebServiceResult(string wUrl)
    {
        ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12;
        HttpWebRequest httpWReq = (HttpWebRequest)WebRequest.Create(wUrl);
        httpWReq.KeepAlive = true;
        httpWReq.ContentType = "application/json";
        httpWReq.Accept = "*/*";
        httpWReq.Method = "GET";
        httpWReq.Headers.Add("Authorization", "Basic M0o4bnRRcWE4Q2g2bnNHM0hsMUI6WA==");
        HttpWebResponse httpWResp = (HttpWebResponse)httpWReq.GetResponse();

        Root[] jsonResponse = null;

        try
        {
            //Test the connection
            if (httpWResp.StatusCode == HttpStatusCode.OK)
            {

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
                root = sr.Deserialize<Root[]>(jsonString.Trim('"'));


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
    public override void PreExecute()
    {
        base.PreExecute();

    }

    /// <summary>Outputs records to the output buffer</summary>
    public override void CreateNewOutputRows()
    {
        //Get SSIS Variables
        //Set Webservice UR.me
        int pageid = Variables.pageid;
        string wUrl = "https://codexltd.freshdesk.com/api/v2/companies?page=" + pageid.ToString();

        try
        {
            //Call getWebServiceResult to return our WorkGroupMetric array
            Root[] outPutMetrics = GetWebServiceResult(wUrl);

            //For each group of metrics output records
            //foreach (var metric in outPutMetrics)
            foreach (var metric in this.root)
            {


                Output0Buffer.AddRow();
                Output0Buffer.id = metric.id;
                Output0Buffer.name = metric.name;
                Output0Buffer.description = metric.description;

                bool isEmpty = IsEmpty(metric.domains);
                if (isEmpty) { } else { Output0Buffer.domains = String.Join(",", metric.domains); }

                if (metric.created_at.HasValue) Output0Buffer.createdat = metric.created_at.Value;
                if (metric.updated_at.HasValue) Output0Buffer.updatedat = metric.updated_at.Value;


            }

        }
        catch (Exception e)
        {
            FailComponent(e.ToString());
        }

    }

}
#endregion

