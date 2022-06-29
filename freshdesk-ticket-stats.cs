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
using Microsoft.SqlServer.Dts.Runtime.Wrapper;
using Microsoft.SqlServer.Server;
#endregion

#region JSON Class

public class Requester
{
    public string id { get; set; }
    public string name { get; set; }
    public string email { get; set; }
}

public class CustomFields
{
    public string cf_fsm_contact_name { get; set; }
    public string cf_fsm_phone_number { get; set; }
    public string cf_fsm_service_location { get; set; }
    public string cf_fsm_appointment_start_time { get; set; }
    public string cf_fsm_appointment_end_time { get; set; }
    public string cf_last_note_added { get; set; }
    public string cf_ticket_type { get; set; }
    public string cf_specific_issue { get; set; }
    public string cf_very_specific_issue { get; set; }
}

public class Stats
{
    //public Stats() { }
    public DateTime? agent_responded_at { get; set; }
    public DateTime? requester_responded_at { get; set; }
    public DateTime? first_responded_at { get; set; }
    public DateTime? status_updated_at { get; set; }
    public DateTime? reopened_at { get; set; }
    public DateTime? resolved_at { get; set; }
    public DateTime? closed_at { get; set; }
    public DateTime? pending_since { get; set; }
}

public class Root
{
    public int id { get; set; }
    public DateTime? due_by { get; set; }
    //public string email { get; set; }
    public DateTime? fr_due_by { get; set; }
    public bool fr_escalated { get; set; }
    public string group_id { get; set; }
    public bool is_escalated { get; set; }
    //public string phone { get; set; }
    public int priority { get; set; }
    public string responder_id { get; set; }
    public int status { get; set; }
    public List<object> tags { get; set; }
    public List<string> to_emails { get; set; }
    public string type { get; set; }
    public DateTime? created_at { get; set; }
    public DateTime? updated_at { get; set; }
    //public Stats statistics { get; set; }
    public Stats stats = new Stats();
    //public Dictionary<string, List<Stats>> stats { get; set; }
    public CustomFields custom_fields { get; set; }
    public Requester requester { get; set; }
    public int? association_type { get; set; }
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

    private Root[] GetWebServiceResult(string wUrl)
    {
        ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12;
        HttpWebRequest httpWReq = (HttpWebRequest)WebRequest.Create(wUrl);
        httpWReq.KeepAlive = true;
        httpWReq.ContentType = "application/json";
        httpWReq.Accept = "*/*";
        httpWReq.Method = "GET";
        httpWReq.Headers.Add("Authorization", varKey);
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


        string strUpdatedAt = Variables.updatedat;
        int intDate = Variables.pageid;
        string wDate = intDate.ToString();


        try
        {
            //Call getWebServiceResult to return our WorkGroupMetric array
            Root[] outPutMetrics = GetWebServiceResult(varUrl);

            //For each group of metrics output records
            //foreach (var metric in outPutMetrics)
            foreach (var metric in this.root)
            {


                Output0Buffer.AddRow();
                Output0Buffer.id = metric.id;
                if (metric.due_by.HasValue) Output0Buffer.dueby = metric.due_by.Value;
                if (metric.fr_due_by.HasValue) Output0Buffer.frdueby = metric.fr_due_by.Value;
                Output0Buffer.frescalated = metric.fr_escalated;
                Output0Buffer.groupid = metric.group_id;
                Output0Buffer.isescalated = metric.is_escalated;
                Output0Buffer.priority = metric.priority;
                Output0Buffer.responderid = metric.responder_id;
                Output0Buffer.status = metric.status;

                bool isEmpty = IsEmpty(metric.to_emails);
                if (isEmpty) { } else { Output0Buffer.toemails = String.Join(",", metric.to_emails); }

                Output0Buffer.type = metric.type;
                if (metric.created_at.HasValue) Output0Buffer.createdat = metric.created_at.Value;
                if (metric.updated_at.HasValue) Output0Buffer.updatedat = metric.updated_at.Value;

                Stats st = metric.stats;
                if (st.first_responded_at.HasValue) Output0Buffer.statsfirstrespondedat = st.first_responded_at.Value;
                if (st.closed_at.HasValue) Output0Buffer.statsclosedat = st.closed_at.Value;
                if (st.pending_since.HasValue) Output0Buffer.statspendingsince = st.pending_since.Value;
                if (st.requester_responded_at.HasValue) Output0Buffer.statsrequesterrespondedat = st.requester_responded_at.Value;
                if (st.resolved_at.HasValue) Output0Buffer.statsresolvedat = st.resolved_at.Value;
                if (st.status_updated_at.HasValue) Output0Buffer.statsstatusupdatedat = st.status_updated_at.Value;
                if (st.reopened_at.HasValue) Output0Buffer.statsreopenedat = st.reopened_at.Value;
                if (st.agent_responded_at.HasValue) Output0Buffer.statsagentrespondedat = st.agent_responded_at.Value;

                CustomFields cf = metric.custom_fields;
                Output0Buffer.cflastnoteadded = metric.custom_fields.cf_last_note_added;
                Output0Buffer.cftickettype = metric.custom_fields.cf_ticket_type;

                if (metric.association_type.HasValue)  Output0Buffer.associationtype = metric.association_type.Value;

            }

        }
        catch (Exception e)
        {
            FailComponent(e.ToString());
        }

    }

}
#endregion

