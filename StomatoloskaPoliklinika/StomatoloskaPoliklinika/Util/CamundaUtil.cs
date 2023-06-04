using Camunda.Api.Client;
using Camunda.Api.Client.History;
using Camunda.Api.Client.Message;
using Camunda.Api.Client.ProcessDefinition;
using Camunda.Api.Client.ProcessInstance;
using Camunda.Api.Client.User;
using Camunda.Api.Client.UserTask;
using StomatoloskaPoliklinika.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace StomatoloskaPoliklinika.Util
{
    public class CamundaUtil
    {
        private const string camundaEngineUri = "http://localhost:8080/engine-rest";
        private static CamundaClient client = CamundaClient.Create(camundaEngineUri);
        private const string processKey = "UgovaranjeSastanka"; //UgovaranjeSastanka

        private const string applyMessage = "UgovaranjeSastankaApplication";


        public static async Task<string> StartUgovaranjeSastankaProcess(int UgovoreniSastanakID, string Pacijent, DateTime DatumVrijeme)
        {
            var parameters = new Dictionary<string, object>();
            parameters["UgovoreniSastanakID"] = UgovoreniSastanakID;
            parameters["Pacijent"] = Pacijent;
            parameters["DatumVrijeme"] = DatumVrijeme;
            var processInstanceId = await StartProcess(parameters);
            return processInstanceId;
        }

        private static async Task<string> StartProcess(Dictionary<string, object> processParameters)
        {
            var client = CamundaClient.Create(camundaEngineUri);
            StartProcessInstance parameters = new StartProcessInstance();
            foreach (var param in processParameters)
            {
                parameters.SetVariable(param.Key, param.Value);
            }
            var definition = client.ProcessDefinitions.ByKey(processKey);
            ProcessInstanceWithVariables processInstance = await definition.StartProcessInstance(parameters);
            return processInstance.Id;
        }

        public static async Task ApplyForUgovaranjeSastanka(string pid, string user)
        {
            var message = new CorrelationMessage()
            {
                ProcessInstanceId = pid,
                MessageName = applyMessage,
                All = true,
                BusinessKey = null
            };
            message.ProcessVariables.Set("Stomatolog", user);
            await client.Messages.DeliverMessage(message);
        }

        public static async Task<bool> IsUserInGroup(string user, string group)
        {
            var list = await client.Users
                                    .Query(new UserQuery
                                    {
                                        Id = user,
                                        MemberOfGroup = group
                                    })
                                    .List();
            return list.Count > 0;
        }

        public static async Task PickSastanak(string taskId, string user)
        {
            await client.UserTasks[taskId].Claim(user);
        }

        public static async Task FinishUgovaranjeSastanka(string taskId)
        {
            await client.UserTasks[taskId].Complete(new CompleteTask());
        }


        public static async Task AssignStomatolog(string taskId, string stomatolog)
        {
            var variables = new Dictionary<string, VariableValue>();
            variables["Stomatolog"] = VariableValue.FromObject(stomatolog);
            await client.UserTasks[taskId].Complete(new CompleteTask()
            {
                Variables = variables
            });
        }

        public static async Task FinishUgovaranjeSastanka(string taskId, bool ok)
        {
            var variables = new Dictionary<string, VariableValue>();
            variables["PregledPotreban"] = VariableValue.FromObject(!ok);
            await client.UserTasks[taskId].Complete(new CompleteTask()
            {
                Variables = variables
            });
        }

        public static async Task<string> GetXmlDefinition()
        {
            var client = CamundaClient.Create(camundaEngineUri);
            var definition = client.ProcessDefinitions.ByKey(processKey);
            ProcessDefinitionDiagram diagram = await definition.GetXml();
            return diagram.Bpmn20Xml;
        }

        //FFU: Za pojedinačni status
        //public static async Task<ReviewInfo> GetInstanceActivities(string instanceId)
        //{
        //  var info = new ReviewInfo();
        //  var instanceHistory = await client.History.ProcessInstances[instanceId].Get();
        //  info.StartTime = instanceHistory.StartTime;
        //  var userTasks = await client.UserTasks.Query(new Camunda.Api.Client.UserTask.TaskQuery() { ProcessInstanceId = instanceId }).List();
        //  var activities = await client.History.ActivityInstances.Query(new Camunda.Api.Client.History.HistoricActivityInstanceQuery
        //  {
        //    ProcessInstanceId = instanceId
        //  }).List();
        //  return info;
        //}

        public static async Task<List<SastanakInfo>> GetUgovoreniSastanci()
        {
            //var list = await client.ProcessInstances.Query(new ProcessInstanceQuery { ProcessDefinitionKey = processKey }).List();
            var historyList = await client.History.ProcessInstances.Query(new HistoricProcessInstanceQuery { ProcessDefinitionKey = processKey }).List();
            var sastanci = historyList.OrderBy(p => p.StartTime)
                                     .Select(p => new SastanakInfo
                                     {
                                         StartTime = p.StartTime,
                                         EndTime = p.State == ProcessInstanceState.Completed ? p.EndTime : new DateTime?(),
                                         Ended = p.State == ProcessInstanceState.Completed,
                                         PID = p.Id
                                     })
                                     .ToList();

            var tasks = new List<Task>();
            foreach (var sastanak in sastanci)
            {
                tasks.Add(LoadInstanceVariables(sastanak));
            }
            await Task.WhenAll(tasks);

            return sastanci;
        }

        public static async Task<List<TaskInfo>> GetTasks(string username)
        {
            var userTasks = await client.UserTasks
                                        .Query(new TaskQuery
                                        {
                                            Assignee = username,
                                            ProcessDefinitionKey = processKey
                                        })
                                        .List();

            var list = userTasks.OrderBy(t => t.Created)
                                .Select(t => new TaskInfo
                                {
                                    TID = t.Id,
                                    TaskName = t.Name,
                                    TaskKey = t.TaskDefinitionKey,
                                    PID = t.ProcessInstanceId,
                                })
                                .ToList();

            var tasks = new List<Task>();
            foreach (var task in list)
            {
                tasks.Add(LoadTaskVariables(task));
            }
            await Task.WhenAll(tasks);
            return list;
        }

        public static async Task<List<TaskInfo>> UnAssignedGroupTasks(string groupName)
        {
            var userTasks = await client.UserTasks
                                        .Query(new TaskQuery
                                        {
                                            Assigned = false,
                                            CandidateGroup = groupName,
                                            ProcessDefinitionKey = processKey
                                        })
                                        .List();

            var list = userTasks.OrderBy(t => t.Created)
                                .Select(t => new TaskInfo
                                {
                                    TID = t.Id,
                                    TaskName = t.Name,
                                    TaskKey = t.TaskDefinitionKey,
                                    PID = t.ProcessInstanceId,
                                })
                                .ToList();

            var tasks = new List<Task>();
            foreach (var task in list)
            {
                tasks.Add(LoadTaskVariables(task));
            }
            await Task.WhenAll(tasks);
            return list;
        }

        private static async Task LoadTaskVariables(TaskInfo task)
        {
            var variables = await client.UserTasks[task.TID].Variables.GetAll();

            if (variables.TryGetValue("UgovoreniSastanakID", out VariableValue value))
            {
                task.UgovoreniSastanakID = value.GetValue<int>();
            }

            if (variables.TryGetValue("Pacijent", out value))
            {
                task.Pacijent = value.GetValue<string>();
            }

            if (variables.TryGetValue("DatumVrijeme", out value))
            {
                task.DatumVrijeme = value.GetValue<DateTime>();
            }

        }

        private static async Task LoadInstanceVariables(SastanakInfo sastanak)
        {
            var list = await client.History.VariableInstances.Query(new HistoricVariableInstanceQuery { ProcessInstanceId = sastanak.PID }).List();
            sastanak.UgovoreniSastanakID = list.Where(v => v.Name == "UgovoreniSastanakID")
                                    .Select(v => Convert.ToInt32(v.Value))
                                    .First();

            sastanak.Pacijent = list.Where(v => v.Name == "Pacijent")
                                    .Select(v => (string)v.Value)
                                    .First();
            sastanak.DatumVrijeme = list.Where(v => v.Name == "DatumVrijeme")
                                    .Select(v => (DateTime)v.Value)
                                    .First();

            var stomatolog = list.Where(v => v.Name == "Stomatolog")
                                 .Select(v => v.Value as string)
                                 .FirstOrDefault();
            sastanak.Stomatolog = stomatolog;

            var timePassed = list.Where(v => v.Name == "TimePassed")
                                  .Select(v => v.Value)
                                  .FirstOrDefault();


            sastanak.PotrebneDorade = string.IsNullOrWhiteSpace(stomatolog) && (timePassed == null || !Convert.ToBoolean(timePassed));
        }
    }
}
