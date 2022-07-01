#region Namespaces
using System;
using Microsoft.SqlServer.Dts.Pipeline.Wrapper;
using System.Net;
using System.IO;
using System.Collections.Generic;
using System.Web.Script.Serialization;
#endregion

#region JSON Class
//Class to hold our work group metrics

public class Contact
{
    public bool active { get; set; }
    public string email { get; set; }
    public string job_title { get; set; }
    public string language { get; set; }
    public DateTime? last_login_at { get; set; }
    public string mobile { get; set; }
    public string name { get; set; }
    public string phone { get; set; }
    public string time_zone { get; set; }
    public DateTime created_at { get; set; }
    public DateTime updated_at { get; set; }
}

public class Root
{
    public bool available { get; set; }
    public bool occasional { get; set; }
    public string id { get; set; }
    public int ticket_scope { get; set; }
    public DateTime created_at { get; set; }
    public DateTime updated_at { get; set; }
    public DateTime? last_active_at { get; set; }
    public DateTime? available_since { get; set; }
    public string type { get; set; }
    public Contact contact { get; set; }
    public string signature { get; set; }
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


        string wUrl = varUrl;

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
                Output0Buffer.contactname = metric.contact.name;
                Output0Buffer.contactactive = metric.contact.active;
                Output0Buffer.contactcreatedat = metric.contact.created_at;
                Output0Buffer.contactemail = metric.contact.email;
                Output0Buffer.contactjobtitle = metric.contact.job_title;
                Output0Buffer.contactlanguage = metric.contact.language;
                if (metric.contact.last_login_at.HasValue) Output0Buffer.contactlastloginat = metric.contact.last_login_at.Value;
                Output0Buffer.contactphone = metric.contact.phone;
                Output0Buffer.contacttimezone = metric.contact.time_zone;
                Output0Buffer.contactupdatedat = metric.contact.updated_at;
                Output0Buffer.type = metric.type;
                if (metric.available_since.HasValue) Output0Buffer.availablesince = metric.available_since.Value;
                if (metric.last_active_at.HasValue) Output0Buffer.lastactiveat = metric.last_active_at.Value;
                Output0Buffer.ticketscope = metric.ticket_scope;

            }

        }
        catch (Exception e)
        {
            FailComponent(e.ToString());
        }

    }

}
#endregion

