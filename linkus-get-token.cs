// To make this script work following variables need to be define:
// - varUsername
// - varPassword
// - varHost
// - varUrl



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
    /// <summary>Outputs records to the output buffer</summary>
    public override void CreateNewOutputRows()
    {
        //Get SSIS Variables
        //Set Webservice URL

        string wUrl = Variables.varUrl;
        try
        {
            //Call getWebServiceResult to return our WorkGroupMetric array
            WorkGroupMetric outPutMetrics = GetWebServiceResult(wUrl);

            {
                Output0Buffer.AddRow();
                Output0Buffer.status = outPutMetrics.status;
                Output0Buffer.token = outPutMetrics.token;
                Output0Buffer.refreshtoken = outPutMetrics.refreshtoken;
                Output0Buffer.transport = outPutMetrics.transport;
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
    private WorkGroupMetric GetWebServiceResult(string wUrl)
    {
        ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12;
        HttpWebRequest httpWReq = (HttpWebRequest)WebRequest.Create(wUrl);
        httpWReq.Method = "POST";
        httpWReq.Host = Variables.varHost;

        string postData = "{\"username\":\"" + Variables.varUsername.ToString() + "\"" + ",\"password\":\"" + Variables.varPassword.ToString() + "\"" + ",\"port\":\"8260\"}";
        UTF8Encoding encoding = new UTF8Encoding();
        byte[] body = encoding.GetBytes(postData);
        httpWReq.ContentLength = body.Length;
        Stream newStream = httpWReq.GetRequestStream();
        newStream.Write(body, 0, body.Length);
        


        HttpWebResponse httpWResp = (HttpWebResponse)httpWReq.GetResponse();

        WorkGroupMetric jsonResponse = null;

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
                jsonResponse = sr.Deserialize<WorkGroupMetric>(jsonString.Trim('"'));

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
class WorkGroupMetric
{
    public string status { get; set; }
    public string token { get; set; }
    public string refreshtoken { get; set; }
    public string transport { get; set; }
}
#endregion