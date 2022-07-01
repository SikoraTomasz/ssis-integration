#region Help:  Introduction to the Script Component
/* The Script Component allows you to perform virtually any operation that can be accomplished in
 * a .Net application within the context of an Integration Services data flow.
 *
 * Expand the other regions which have "Help" prefixes for examples of specific ways to use
 * Integration Services features within this script component. */
#endregion

#region Namespaces
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
#endregion
#endregion

/// <summary>
/// This is the class to which to add your code.  Do not change the name, attributes, or parent
/// of this class.
/// </summary>
[Microsoft.SqlServer.Dts.Pipeline.SSISScriptComponentEntryPointAttribute]
public class ScriptMain : UserComponent
{

    Root[] root;
    #region Help:  Using Integration Services variables and parameters
    /* To use a variable in this script, first ensure that the variable has been added to
     * either the list contained in the ReadOnlyVariables property or the list contained in
     * the ReadWriteVariables property of this script component, according to whether or not your
     * code needs to write into the variable.  To do so, save this script, close this instance of
     * Visual Studio, and update the ReadOnlyVariables and ReadWriteVariables properties in the
     * Script Transformation Editor window.
     * To use a parameter in this script, follow the same steps. Parameters are always read-only.
     *
     * Example of reading from a variable or parameter:
     *  DateTime startTime = Variables.MyStartTime;
     *
     * Example of writing to a variable:
     *  Variables.myStringVariable = "new value";
     */
    #endregion

    #region Help:  Using Integration Services Connnection Managers
    /* Some types of connection managers can be used in this script component.  See the help topic
     * "Working with Connection Managers Programatically" for details.
     *
     * To use a connection manager in this script, first ensure that the connection manager has
     * been added to either the list of connection managers on the Connection Managers page of the
     * script component editor.  To add the connection manager, save this script, close this instance of
     * Visual Studio, and add the Connection Manager to the list.
     *
     * If the component needs to hold a connection open while processing rows, override the
     * AcquireConnections and ReleaseConnections methods.
     * 
     * Example of using an ADO.Net connection manager to acquire a SqlConnection:
     *  object rawConnection = Connections.SalesDB.AcquireConnection(transaction);
     *  SqlConnection salesDBConn = (SqlConnection)rawConnection;
     *
     * Example of using a File connection manager to acquire a file path:
     *  object rawConnection = Connections.Prices_zip.AcquireConnection(transaction);
     *  string filePath = (string)rawConnection;
     *
     * Example of releasing a connection manager:
     *  Connections.SalesDB.ReleaseConnection(rawConnection);
     */
    #endregion

    #region Help:  Firing Integration Services Events
    /* This script component can fire events.
     *
     * Example of firing an error event:
     *  ComponentMetaData.FireError(10, "Process Values", "Bad value", "", 0, out cancel);
     *
     * Example of firing an information event:
     *  ComponentMetaData.FireInformation(10, "Process Values", "Processing has started", "", 0, fireAgain);
     *
     * Example of firing a warning event:
     *  ComponentMetaData.FireWarning(10, "Process Values", "No rows were received", "", 0);
     */
    #endregion

    /// <summary>
    /// This method is called once, before rows begin to be processed in the data flow.
    ///
    /// You can remove this method if you don't need to do anything here.
    /// </summary>
    public override void PreExecute()
    {
        base.PreExecute();
        /*
         * Add your code here
         */
    }

    /// <summary>
    /// This method is called after all the rows have passed through this component.
    ///
    /// You can delete this method if you don't need to do anything here.
    /// </summary>
    public override void PostExecute()
    {
        base.PostExecute();
        /*
         * Add your code here
         */
    }

    /// <summary>
    /// This method is called once for every row that passes through the component from Input0.
    ///
    /// Example of reading a value from a column in the the row:
    ///  string zipCode = Row.ZipCode
    ///
    /// Example of writing a value to a column in the row:
    ///  Row.ZipCode = zipCode
    /// </summary>
    /// <param name="Row">The row that is currently passing through the component</param>
    public override void Input0_ProcessInputRow(Input0Buffer Row)
    {
        //Get SSIS Variables
        string strToken = Row.token;
        //Set Webservice URL
        string wUrl = "https://codex.ntcloudpbx.ie/api/v2.0.0/call/query?token=" + strToken;
        //string strCallID;
        try
        {
            //Call getWebServiceResult to return our WorkGroupMetric array
            Root outPutMetrics = GetWebServiceResult(wUrl);




            if (outPutMetrics.Calls != null)
            {


                //For each group of metrics output records
                foreach (var metric in outPutMetrics.Calls)
                {
                    Output0Buffer.AddRow();
                    Output0Buffer.callid = metric.callid;
                    Output0Buffer.callpath = metric.members[0].inbound.callpath;
                    Output0Buffer.channelid = metric.members[0].inbound.channelid;
                    //Output0Buffer.extchannelid = metric.members[0].ext.channelid;
                    //Output0Buffer.extmemberstatus = metric.members[0].ext.memberstatus;
                    //Output0Buffer.extnumber = metric.members[1].ext.number;
                    Output0Buffer.from = metric.members[0].inbound.from;
                    Output0Buffer.memberstatus = metric.members[0].inbound.memberstatus;
                    Output0Buffer.to = metric.members[0].inbound.to;
                    Output0Buffer.trunkname = metric.members[0].inbound.trunkname;
                }


            }

        }



        catch (Exception e)
        {
            FailComponent(e.ToString());
        }

    }

    private Root GetWebServiceResult(string wUrl)
    {
        ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12;
        HttpWebRequest httpWReq = (HttpWebRequest)WebRequest.Create(wUrl);
        httpWReq.Method = "POST";
        httpWReq.Host = "codex.ntcloudpbx.ie";

        string postData = "{\"callid\":\"all\"}";
        UTF8Encoding encoding = new UTF8Encoding();
        byte[] body = encoding.GetBytes(postData);
        httpWReq.ContentLength = body.Length;
        Stream newStream = httpWReq.GetRequestStream();
        newStream.Write(body, 0, body.Length);

        HttpWebResponse httpWResp = (HttpWebResponse)httpWReq.GetResponse();
        Root jsonResponse = null;

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
                jsonResponse = sr.Deserialize<Root>(jsonString.Trim('"'));

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
    private void FailComponent(string errorMsg)
    {
        bool fail = false;
        IDTSComponentMetaData100 compMetadata = this.ComponentMetaData;
        compMetadata.FireError(1, "Error Getting Data From Webservice!", errorMsg, "", 0, out fail);

    }

}
#region JSON Class
public class Inbound
{
    public string from { get; set; }
    public string to { get; set; }
    public string trunkname { get; set; }
    public string channelid { get; set; }
    public string memberstatus { get; set; }
    public string callpath { get; set; }
}

public class Ext
{
    public string number { get; set; }
    public string channelid { get; set; }
    public string memberstatus { get; set; }
}

public class Member
{
    public Inbound inbound { get; set; }
    //public Inbound inbound = new Inbound();
    public Ext ext { get; set; }
    //public Ext ext = new Ext();
}

public class Call
{
    public List<Member> members { get; set; }
    public string callid { get; set; }
}

public class Root
{
    public string status { get; set; }
    public List<Call> Calls { get; set; }
}
#endregion