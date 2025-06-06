namespace RagConfluenceDotNet.Models;

public class ConfluenceSpaceJson
{
    public class Rootobject
    {
        public Result[] results { get; set; }
        public int start { get; set; }
        public int limit { get; set; }
        public int size { get; set; }
        public _Links _links { get; set; }
    }

    public class _Links
    {
        public string _base { get; set; }
        public string context { get; set; }
        public string self { get; set; }
    }

    public class Result
    {
        public int id { get; set; }
        public string key { get; set; }
        public string name { get; set; }
        public string type { get; set; }
        public string status { get; set; }
        public _Expandable _expandable { get; set; }
        public _Links1 _links { get; set; }
    }

    public class _Expandable
    {
        public string settings { get; set; }
        public string metadata { get; set; }
        public string operations { get; set; }
        public string lookAndFeel { get; set; }
        public string identifiers { get; set; }
        public string permissions { get; set; }
        public string icon { get; set; }
        public string description { get; set; }
        public string theme { get; set; }
        public string history { get; set; }
        public string homepage { get; set; }
    }

    public class _Links1
    {
        public string webui { get; set; }
        public string self { get; set; }
    }
}