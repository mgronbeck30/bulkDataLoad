using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Pfe.Xrm;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Messages;
using System.IO;
using Newtonsoft.Json;
namespace Microsoft.Pfe.Xrm.Samples
{
    class Program
    {
        List<string> accountIDs = new List<string>();
        List<string> userIDs = new List<string>();
        List<Guid> newEnts = new List<Guid>();

        OrganizationServiceManager Manager { get; set; }

        public Program(Uri serverUri, string username, string password)
        {
            this.Manager = new OrganizationServiceManager(serverUri, username, password);
        }
        static void Main(string[] args)
        {
            StreamReader sr = new StreamReader(".\\Accounts.json");
            var allAccountData = JsonConvert.DeserializeObject<Dictionary<string, object>>(sr.ReadToEnd());
            var allValues = JsonConvert.DeserializeObject<List<Dictionary<string, string>>>(allAccountData["value"].ToString());
            var accountList = new List<string>();
            foreach (var acct in allValues)
            {
                accountList.Add(acct["accountid"].ToString());
            }
            sr = new StreamReader(".\\Users1.json");
            var allUserData = JsonConvert.DeserializeObject<Dictionary<string, object>>(sr.ReadToEnd());
            allValues = JsonConvert.DeserializeObject<List<Dictionary<string, string>>>(allUserData["value"].ToString());
            var userList = new List<string>();
            foreach(var user in allValues)
            {
                userList.Add(user["systemuserid"].ToString());
            }

            sr = new StreamReader(".\\Users2.json");
            allUserData = JsonConvert.DeserializeObject<Dictionary<string, object>>(sr.ReadToEnd());
            allValues = JsonConvert.DeserializeObject<List<Dictionary<string, string>>>(allUserData["value"].ToString());

            foreach (var user in allValues)
            {
                userList.Add(user["systemuserid"].ToString());
            }
            var serverUri = XrmServiceUriFactory.CreateOrganizationServiceUri("https://<organization>.crm.dynamics.com");
            Program p1 = new Program(serverUri,"user@domain.com","xxxxxxxx");
            p1.accountIDs = accountList;
            p1.userIDs = userList;
            allAccountData = null;
            allValues = null;
            accountList = null;
            userList = null;
            allUserData = null;
            StreamWriter outfile = new StreamWriter(".\\output2.csv");
            List<ExecuteMultipleRequest> newTeamMembers = p1.buildEntities(p1.accountIDs,p1.userIDs);
            try
            {
                var options = new OrganizationServiceProxyOptions()
                {
                    Timeout = new TimeSpan(2, 0, 0)
                };
                Console.WriteLine(DateTime.Now);
                var output = p1.Manager.ParallelProxy.Execute(newTeamMembers,options).ToList();
                Console.WriteLine(DateTime.Now);
                Console.WriteLine("WritingOutputFile");
                foreach(Guid x in p1.newEnts)
                {
                    outfile.WriteLine(x.ToString());
                }
                outfile.Close();
                Console.WriteLine("OutputFileWritten output.csv");
                Console.ReadLine();
            }
            catch(AggregateException ae)
            {
                Console.WriteLine(DateTime.Now);
                Console.WriteLine("WritingOutputFile");
                foreach (Guid x in p1.newEnts)
                {
                    outfile.WriteLine(x.ToString());
                }
                outfile.Close();
                Console.WriteLine("OutputFileWritten output.csv");
                throw;
                Console.ReadLine();
            }
            //execute messages
        }
        public List<ExecuteMultipleRequest> buildEntities(List<string> accountIds, List<string> userIds )
        {
            EntityCollection returnCollection = new EntityCollection();
            List<ExecuteMultipleRequest> emrs = new List<ExecuteMultipleRequest>();
            int l = 0;
            ExecuteMultipleRequest emr = new ExecuteMultipleRequest();
            emr.Settings = new ExecuteMultipleSettings()
            {
                ContinueOnError = true,
                ReturnResponses = true
            };
            emr.Requests = new OrganizationRequestCollection();
            for (int i = 0; i < 200000; i++)
            {
                if(l == 10)
                {
                    emrs.Add(emr);
                    emr = new ExecuteMultipleRequest();
                    emr.Requests = new OrganizationRequestCollection();
                    emr.Settings = new ExecuteMultipleSettings()
                    {
                        ContinueOnError = true,
                        ReturnResponses = true
                    };
                    l = 0;
                }
                Random rand = new Random();
                int j = rand.Next(accountIds.Count - 1);
                int k = rand.Next(userIds.Count - 1);
                CreateRequest create = new CreateRequest();
                Entity teammember = new Entity("qst_accountteammember");
                teammember.Id = Guid.NewGuid();
                newEnts.Add(teammember.Id);
                teammember.Attributes.Add(new KeyValuePair<string, object>("qst_name", "Created by Load Test at " + DateTime.Now.ToLongDateString()));
                teammember.Attributes.Add(new KeyValuePair<string, object>("qst_accountid", new EntityReference("account", new Guid(accountIDs[j]))));
                teammember.Attributes.Add(new KeyValuePair<string, object>("qst_userid", new EntityReference("systemuser", new Guid(userIDs[k]))));
                teammember.Attributes.Add(new KeyValuePair<string, object>("qst_roleid", new EntityReference("qst_accountteamrole", new Guid("B7C288D9-20F1-E811-A974-000D3A1A941E"))));
                returnCollection.Entities.Add(teammember);
                create.Target = teammember;
                emr.Requests.Add(create);
                l++;
            }
            return emrs;
        }
    }
}
