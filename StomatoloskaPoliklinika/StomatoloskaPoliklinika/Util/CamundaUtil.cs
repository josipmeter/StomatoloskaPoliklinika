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
        private const string processKey = "QuestionReview"; //UgovaranjeSastanka

        private const string applyMessage = "ReviewApplication";


        public static async Task<string> StartReviewProcess(int questionId, string author)
        {
            var parameters = new Dictionary<string, object>();
            parameters["QuestionId"] = questionId;
            parameters["Author"] = author;
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

        public static async Task ApplyForReview(string pid, string user)
        {
            var message = new CorrelationMessage()
            {
                ProcessInstanceId = pid,
                MessageName = applyMessage,
                All = true,
                BusinessKey = null
            };
            message.ProcessVariables.Set("Reviewer", user);
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

        public static async Task PickTask(string taskId, string user)
        {
            await client.UserTasks[taskId].Claim(user);
        }

        public static async Task FinishTask(string taskId)
        {
            await client.UserTasks[taskId].Complete(new CompleteTask());
        }


        public static async Task AssignReviewer(string taskId, string reviewer)
        {
            var variables = new Dictionary<string, VariableValue>();
            variables["Reviewer"] = VariableValue.FromObject(reviewer);
            await client.UserTasks[taskId].Complete(new CompleteTask()
            {
                Variables = variables
            });
        }

        public static async Task FinishReview(string taskId, string comment, bool ok)
        {
            var variables = new Dictionary<string, VariableValue>();
            variables["RevisionNeeded"] = VariableValue.FromObject(!ok);
            variables["Comment"] = VariableValue.FromObject(comment);
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

        public static async Task<List<ReviewInfo>> GetReviews()
        {
            //var list = await client.ProcessInstances.Query(new ProcessInstanceQuery { ProcessDefinitionKey = processKey }).List();
            var historyList = await client.History.ProcessInstances.Query(new HistoricProcessInstanceQuery { ProcessDefinitionKey = processKey }).List();
            var reviews = historyList.OrderBy(p => p.StartTime)
                                     .Select(p => new ReviewInfo
                                     {
                                         StartTime = p.StartTime,
                                         EndTime = p.State == ProcessInstanceState.Completed ? p.EndTime : new DateTime?(),
                                         Ended = p.State == ProcessInstanceState.Completed,
                                         PID = p.Id
                                     })
                                     .ToList();

            var tasks = new List<Task>();
            foreach (var review in reviews)
            {
                tasks.Add(LoadInstanceVariables(review));
            }
            await Task.WhenAll(tasks);

            return reviews;
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

            if (variables.TryGetValue("QuestionId", out VariableValue value))
            {
                task.UgovoreniSastanakId = value.GetValue<int>();
            }

            if (variables.TryGetValue("Author", out value))
            {
                task.Pacijent = value.GetValue<string>();
            }

        }

        private static async Task LoadInstanceVariables(ReviewInfo review)
        {
            var list = await client.History.VariableInstances.Query(new HistoricVariableInstanceQuery { ProcessInstanceId = review.PID }).List();
            review.UgovoreniSastanakId = list.Where(v => v.Name == "QuestionId")
                                    .Select(v => Convert.ToInt32(v.Value))
                                    .First();

            review.Pacijent = list.Where(v => v.Name == "Author")
                                    .Select(v => (string)v.Value)
                                    .First();

            var reviewer = list.Where(v => v.Name == "Reviewer")
                                 .Select(v => v.Value as string)
                                 .FirstOrDefault();
            review.Stomatolog = reviewer;

            var timePassed = list.Where(v => v.Name == "TimePassed")
                                  .Select(v => v.Value)
                                  .FirstOrDefault();


            review.CanApplyForReview = string.IsNullOrWhiteSpace(reviewer) && (timePassed == null || !Convert.ToBoolean(timePassed));
        }
    }
}
