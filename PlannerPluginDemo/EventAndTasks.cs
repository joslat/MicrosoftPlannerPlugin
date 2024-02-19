using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PlannerPluginDemo;

public class EventPlan
{
    public string Bucketname { get; set; }
    public List<TaskItem> Tasklist { get; set; }
}

public class TaskItem
{
    public string Task { get; set; }
}
